using SistemaHospitalar.Application.DTOs.Ai;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Services;

internal static class TriageAdmissionHelper
{
    public const int WindowHours = 72;

    public static (string Reason, string? Diagnosis) BuildAdmissionFields(AiTriageLog triage)
    {
        var reason = triage.Symptoms.Trim();
        string? diagnosis = null;

        if (!string.IsNullOrWhiteSpace(triage.SuggestedCid10))
        {
            diagnosis = string.IsNullOrWhiteSpace(triage.SuggestedCid10Description)
                ? triage.SuggestedCid10.Trim()
                : $"{triage.SuggestedCid10.Trim()} — {triage.SuggestedCid10Description.Trim()}";
        }

        return (reason, diagnosis);
    }

    public static TriageAdmissionSuggestionDto ToSuggestionDto(AiTriageLog triage)
    {
        var meta = ManchesterMetadata(triage.Urgency);
        var fields = BuildAdmissionFields(triage);

        return new TriageAdmissionSuggestionDto(
            triage.Id,
            fields.Reason,
            fields.Diagnosis,
            triage.Urgency,
            meta.Label,
            meta.Color,
            triage.RecommendedSpecialty,
            triage.SuggestedCid10,
            triage.SuggestedCid10Description,
            triage.CreatedAt);
    }

    public static string? AppendTriageNotes(string? notes, AiTriageLog triage)
    {
        var meta = ManchesterMetadata(triage.Urgency);
        var triageNote =
            $"Triagem IA ({meta.Color} — {meta.Label}) em {triage.CreatedAt:dd/MM/yyyy HH:mm}. " +
            $"Especialidade sugerida: {triage.RecommendedSpecialty}.";

        return string.IsNullOrWhiteSpace(notes)
            ? triageNote
            : $"{notes.Trim()}\n{triageNote}";
    }

    private static (string Label, string Color) ManchesterMetadata(TriageUrgency urgency) => urgency switch
    {
        TriageUrgency.Emergency => ("Emergência", "Vermelho"),
        TriageUrgency.High => ("Muito Urgente", "Laranja"),
        TriageUrgency.Medium => ("Urgente", "Amarelo"),
        TriageUrgency.Low => ("Pouco Urgente", "Verde"),
        TriageUrgency.NonUrgent => ("Não Urgente", "Azul"),
        _ => ("Urgente", "Amarelo"),
    };
}
