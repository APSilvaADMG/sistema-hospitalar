using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Integrations;
using SistemaHospitalar.Application.DTOs.Patients;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

public class IntegrationService(
    AppDbContext dbContext,
    IPatientService patientService,
    IFieldEncryptionService encryption) : IIntegrationService
{
    public async Task<IReadOnlyList<IntegrationMessageDto>> GetMessagesAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var messages = await dbContext.IntegrationMessages
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new
            {
                m.Id,
                m.Type,
                m.Status,
                m.Source,
                m.Destination,
                m.Payload,
                m.ErrorMessage,
                PatientName = m.Patient != null ? m.Patient.FullName : null,
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return messages
            .Select(m => new IntegrationMessageDto(
                m.Id,
                m.Type,
                m.Status,
                m.Source,
                m.Destination,
                m.Payload.Length > 120 ? m.Payload[..120] + "..." : m.Payload,
                m.ErrorMessage,
                m.PatientName,
                m.CreatedAt))
            .ToList();
    }

    public async Task<IntegrationProcessResultDto> ProcessHl7InboundAsync(
        Hl7InboundRequest request, CancellationToken cancellationToken = default)
    {
        var message = new IntegrationMessage
        {
            Type = IntegrationMessageType.Hl7Inbound,
            Source = request.Source.Trim(),
            Payload = request.RawMessage.Trim()
        };

        try
        {
            var summary = ParseHl7Summary(request.RawMessage);
            message.Status = IntegrationMessageStatus.Processed;
            message.ResponsePayload = summary;
            dbContext.IntegrationMessages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new IntegrationProcessResultDto(message.Id, message.Status, summary, null);
        }
        catch (Exception ex)
        {
            message.Status = IntegrationMessageStatus.Failed;
            message.ErrorMessage = ex.Message;
            dbContext.IntegrationMessages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new IntegrationProcessResultDto(message.Id, message.Status, null, null);
        }
    }

    public async Task<FhirPatientExportDto?> ExportFhirPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            return null;
        }

        PatientFieldProtection.Decrypt(patient, encryption);

        var fhir = new
        {
            resourceType = "Patient",
            id = patient.Id.ToString(),
            identifier = new[] { new { system = "urn:cpf", value = patient.Cpf } },
            name = new[] { new { use = "official", text = patient.FullName } },
            birthDate = patient.BirthDate.ToString("yyyy-MM-dd"),
            gender = patient.Gender switch
            {
                Gender.Male => "male",
                Gender.Female => "female",
                _ => "unknown"
            },
            telecom = new[]
            {
                patient.Phone != null ? new { system = "phone", value = patient.Phone } : null,
                patient.Email != null ? new { system = "email", value = patient.Email } : null
            }.Where(t => t != null)
        };

        var json = JsonSerializer.Serialize(fhir, new JsonSerializerOptions { WriteIndented = true });

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.FhirExport,
            Source = "SistemaHospitalar",
            Destination = "FHIR/R4",
            Payload = json,
            Status = IntegrationMessageStatus.Processed,
            PatientId = patientId,
            ResponsePayload = "Exportação FHIR Patient concluída"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FhirPatientExportDto("Patient", patient.Id.ToString(), json);
    }

    public async Task<IntegrationProcessResultDto> ImportFhirPatientAsync(
        string fhirJson, CancellationToken cancellationToken = default)
    {
        var message = new IntegrationMessage
        {
            Type = IntegrationMessageType.FhirImport,
            Source = "FHIR/R4",
            Destination = "SistemaHospitalar",
            Payload = fhirJson
        };

        try
        {
            using var doc = JsonDocument.Parse(fhirJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("resourceType", out var rt) || rt.GetString() != "Patient")
            {
                throw new InvalidOperationException("Recurso FHIR deve ser do tipo Patient.");
            }

            var name = root.GetProperty("name")[0].GetProperty("text").GetString() ?? "Paciente FHIR";
            var cpf = root.GetProperty("identifier")[0].GetProperty("value").GetString() ?? string.Empty;
            cpf = PatientCpfRules.Normalize(cpf);
            if (cpf.Length < 11)
            {
                cpf = cpf.PadLeft(11, '0');
            }

            PatientCpfRules.ValidateFormat(cpf);

            var cpfHash = encryption.HashForLookup(cpf);
            var existing = await dbContext.Patients
                .FirstOrDefaultAsync(
                    p => !p.UsesResponsibleCpf && p.CpfHash == cpfHash,
                    cancellationToken);

            if (existing is not null)
            {
                message.PatientId = existing.Id;
                message.Status = IntegrationMessageStatus.Processed;
                message.ResponsePayload = $"Paciente com CPF {cpf} já existe.";
            }
            else
            {
                var birthDate = root.TryGetProperty("birthDate", out var bd)
                    ? DateOnly.Parse(bd.GetString()!)
                    : DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30));

                Gender gender = Gender.NotInformed;
                if (root.TryGetProperty("gender", out var genderElement))
                {
                    gender = genderElement.GetString() switch
                    {
                        "male" => Gender.Male,
                        "female" => Gender.Female,
                        _ => Gender.NotInformed
                    };
                }

                var createRequest = new CreatePatientRequest(
                    name,
                    null,
                    cpf,
                    birthDate,
                    gender,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);

                var result = await patientService.CreateAsync(createRequest, cancellationToken);
                message.PatientId = result.Patient.Id;
                message.Status = IntegrationMessageStatus.Processed;
                message.ResponsePayload = $"Paciente {name} importado via FHIR.";
            }

            dbContext.IntegrationMessages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new IntegrationProcessResultDto(message.Id, message.Status, message.ResponsePayload, message.PatientId);
        }
        catch (Exception ex)
        {
            message.Status = IntegrationMessageStatus.Failed;
            message.ErrorMessage = ex.Message;
            dbContext.IntegrationMessages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static string ParseHl7Summary(string raw)
    {
        var lines = raw.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var pid = lines.FirstOrDefault(l => l.StartsWith("PID|", StringComparison.OrdinalIgnoreCase));
        if (pid is null)
        {
            return "Mensagem HL7 recebida (sem segmento PID).";
        }

        var fields = pid.Split('|');
        var patientName = fields.Length > 5 ? fields[5].Replace('^', ' ').Trim() : "Desconhecido";
        var messageType = lines[0].Split('|').Length > 8 ? lines[0].Split('|')[8] : "UNKNOWN";
        return $"HL7 {messageType} — Paciente: {patientName}";
    }
}
