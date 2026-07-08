namespace SistemaHospitalar.Application.DTOs.Bedside;

public record BedsideVitalsRequest(
    string IdentityCode,
    string? BloodPressure,
    string? HeartRate,
    string? RespiratoryRate,
    string? Temperature,
    string? SpO2,
    string Password);

public record BedsideMedicationRequest(
    string IdentityCode,
    Guid? PrescriptionEntryId,
    string MedicationName,
    string Dose,
    string Route,
    string Password);

public record BedsideCareResultDto(
    Guid EntryId,
    bool IsSigned,
    DateTime CreatedAt,
    string Message);
