using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Dados clínicos de demonstração para o paciente Feegow (João da Silva) — alimenta seções do PEP na recepção.
/// </summary>
public static class FeegowPatientPepSeed
{
    private const string DemoPatientCpf = "52998224725";
    private const string SeedMarker = "seed-feegow-pep-demo";

    public static async Task EnsureAsync(
        AppDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.MedicalRecordEntries.AnyAsync(e => e.ClientRequestId == SeedMarker, cancellationToken))
        {
            return;
        }

        var patient = await dbContext.Patients
            .Include(p => p.MedicalRecord)
            .FirstOrDefaultAsync(p => p.Cpf == DemoPatientCpf, cancellationToken);

        if (patient?.MedicalRecord is null)
        {
            return;
        }

        var professional = await dbContext.Professionals
            .FirstOrDefaultAsync(p => p.Email == "ana.silva@hospital.local", cancellationToken);

        if (professional is null)
        {
            return;
        }

        var recordId = patient.MedicalRecord.Id;
        var now = DateTime.UtcNow;
        var signedAt = now.AddDays(-2);

        dbContext.MedicalRecordEntries.AddRange(
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Anamnesis,
                Content = "Queixa principal: dor torácica em aperto há 2 dias, piora aos esforços.\n"
                    + "HMA: nega febre. HAS em uso de losartana. Tabagista 10 maços/ano.",
                ClientRequestId = SeedMarker,
                CreatedAt = now.AddDays(-5),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Evolution,
                Content = "Evolução: paciente estável, sem dor em repouso. PA 130/80. FC 72 bpm.",
                Cid10Code = "J06.9",
                ClientRequestId = SeedMarker,
                IsSigned = true,
                SignedAt = signedAt,
                SignedByProfessionalId = professional.Id,
                CreatedAt = now.AddDays(-3),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Evolution,
                Content = "Hipótese: angina estável. Solicitados ECG e troponina. Orientado repouso relativo.",
                Cid10Code = "I20.9",
                ClientRequestId = SeedMarker,
                CreatedAt = now.AddDays(-2),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Procedure,
                Content = "Encaminho o(a) paciente para avaliação especializada em cardiologia — protocolo de dor torácica.",
                ClientRequestId = SeedMarker,
                CreatedAt = now.AddDays(-2),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Prescription,
                Content = "AAS 100mg — 1 comprimido via oral, 1x ao dia.\n"
                    + "Sinvastatina 20mg — 1 comprimido via oral, à noite.\n"
                    + "Retorno em 30 dias ou se piora.",
                Cid10Code = "I20.9",
                ClientRequestId = SeedMarker,
                IsSigned = true,
                SignedAt = signedAt,
                SignedByProfessionalId = professional.Id,
                CreatedAt = now.AddDays(-1),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.ExamRequest,
                Content = "Solicito: hemograma completo, glicemia em jejum, creatinina, ECG.",
                ClientRequestId = SeedMarker,
                CreatedAt = now.AddDays(-1),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Evolution,
                Content = "Atesto para os devidos fins que o(a) paciente necessita de afastamento de suas atividades por 2 dia(s), a contar desta data.",
                ClientRequestId = SeedMarker,
                CreatedAt = now.AddHours(-6),
            },
            new MedicalRecordEntry
            {
                MedicalRecordId = recordId,
                ProfessionalId = professional.Id,
                EntryType = MedicalRecordEntryType.Evolution,
                Content = "[anexo] ECG de repouso — ritmo sinusal, sem supradesnível de ST. Arquivo digital disponível no PACS interno.",
                ClientRequestId = SeedMarker,
                CreatedAt = now.AddHours(-4),
            });

        await EnsureLabOrderAsync(dbContext, patient.Id, professional.Id, now, cancellationToken);
        await EnsureImagingStudyAsync(dbContext, patient.Id, professional.Id, now, cancellationToken);
        await EnsureAppointmentAsync(dbContext, patient.Id, professional.Id, now, cancellationToken);
        await EnsureVaccinationAsync(dbContext, patient.Id, professional.Id, now, cancellationToken);
        await EnsureDispensingAsync(dbContext, patient.Id, professional.Id, now, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Dados PEP Feegow aplicados para paciente demo (CPF {Cpf}).", DemoPatientCpf);
    }

    private static async Task EnsureLabOrderAsync(
        AppDbContext dbContext,
        Guid patientId,
        Guid professionalId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.LabOrders.AnyAsync(o => o.Notes == SeedMarker, cancellationToken))
        {
            return;
        }

        var exams = await dbContext.LabExamCatalogs
            .Where(e => e.Name == "Hemograma completo" || e.Name == "Glicemia em jejum")
            .ToListAsync(cancellationToken);

        if (exams.Count == 0)
        {
            return;
        }

        var order = new LabOrder
        {
            PatientId = patientId,
            RequestingProfessionalId = professionalId,
            Status = LabOrderStatus.Completed,
            Notes = SeedMarker,
            CreatedAt = now.AddDays(-1),
        };

        foreach (var exam in exams)
        {
            var item = new LabOrderItem
            {
                LabExamCatalogId = exam.Id,
                Status = LabItemStatus.Completed,
            };

            if (exam.Name == "Hemograma completo")
            {
                item.Result = new LabResult
                {
                    Value = "13,2",
                    Unit = "g/dL",
                    ReferenceRange = "12,0-16,0",
                    ReleasedAt = now.AddHours(-12),
                };
            }
            else
            {
                item.Result = new LabResult
                {
                    Value = "98",
                    Unit = "mg/dL",
                    ReferenceRange = "70-99",
                    ReleasedAt = now.AddHours(-12),
                };
            }

            order.Items.Add(item);
        }

        dbContext.LabOrders.Add(order);

        var pendingOrder = new LabOrder
        {
            PatientId = patientId,
            RequestingProfessionalId = professionalId,
            Status = LabOrderStatus.Requested,
            Notes = $"{SeedMarker}-pending",
            CreatedAt = now.AddHours(-2),
        };

        var creatinina = await dbContext.LabExamCatalogs
            .FirstOrDefaultAsync(e => e.Name == "Creatinina", cancellationToken);

        if (creatinina is not null)
        {
            pendingOrder.Items.Add(new LabOrderItem
            {
                LabExamCatalogId = creatinina.Id,
                Status = LabItemStatus.Pending,
            });
            dbContext.LabOrders.Add(pendingOrder);
        }
    }

    private static async Task EnsureImagingStudyAsync(
        AppDbContext dbContext,
        Guid patientId,
        Guid professionalId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ImagingStudies.AnyAsync(s => s.AccessionNumber == SeedMarker, cancellationToken))
        {
            return;
        }

        dbContext.ImagingStudies.Add(new ImagingStudy
        {
            PatientId = patientId,
            RequestingProfessionalId = professionalId,
            ReportingProfessionalId = professionalId,
            Modality = ImagingModality.XRay,
            StudyDescription = "Radiografia de tórax — PA e perfil",
            Status = ImagingStudyStatus.Completed,
            ScheduledAt = now.AddDays(-1),
            CompletedAt = now.AddHours(-8),
            ReportContent = "Campos pulmonares livres. Área cardíaca dentro dos limites da normalidade. Seios costofrênicos preservados.",
            ReportedAt = now.AddHours(-6),
            AccessionNumber = SeedMarker,
        });
    }

    private static async Task EnsureAppointmentAsync(
        AppDbContext dbContext,
        Guid patientId,
        Guid professionalId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Appointments.AnyAsync(a => a.Notes == SeedMarker, cancellationToken))
        {
            return;
        }

        dbContext.Appointments.Add(new Appointment
        {
            PatientId = patientId,
            ProfessionalId = professionalId,
            ScheduledAt = now.AddDays(7),
            DurationMinutes = 30,
            Status = AppointmentStatus.Scheduled,
            Reason = "Retorno cardiologia — revisão de exames",
            Room = "Consultório 102",
            Notes = SeedMarker,
        });
    }

    private static async Task EnsureVaccinationAsync(
        AppDbContext dbContext,
        Guid patientId,
        Guid professionalId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.PatientVaccinations.AnyAsync(v => v.Notes == SeedMarker, cancellationToken))
        {
            return;
        }

        var vaccine = await dbContext.VaccineCatalogs
            .Where(v => v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (vaccine is null)
        {
            return;
        }

        dbContext.PatientVaccinations.Add(new PatientVaccination
        {
            PatientId = patientId,
            VaccineCatalogId = vaccine.Id,
            ProfessionalId = professionalId,
            AdministeredAt = now.AddMonths(-6),
            DoseNumber = 1,
            BatchNumber = "LOTE-DEMO-2025",
            Notes = SeedMarker,
        });
    }

    private static async Task EnsureDispensingAsync(
        AppDbContext dbContext,
        Guid patientId,
        Guid professionalId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.PharmacyDispensings.AnyAsync(d => d.Notes == SeedMarker, cancellationToken))
        {
            return;
        }

        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Sku == "MED-OME20", cancellationToken);

        if (product is null)
        {
            return;
        }

        dbContext.PharmacyDispensings.Add(new PharmacyDispensing
        {
            PatientId = patientId,
            ProductId = product.Id,
            ProfessionalId = professionalId,
            Quantity = 30,
            DispensedAt = now.AddDays(-1),
            Notes = SeedMarker,
        });
    }
}
