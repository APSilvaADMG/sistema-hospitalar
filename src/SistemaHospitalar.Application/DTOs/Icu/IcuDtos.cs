using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Icu;

public record IcuPatientDto(
    Guid HospitalizationId,
    Guid PatientId,
    string PatientName,
    string BedNumber,
    string WardName,
    VitalSignDto? LatestVitals,
    string AlertLevel);

public record VitalSignDto(
    Guid Id,
    int HeartRate,
    int SystolicBp,
    int DiastolicBp,
    int SpO2,
    decimal Temperature,
    int RespiratoryRate,
    DateTime RecordedAt,
    string? RecordedByName);

public record RecordVitalSignsRequest(
    Guid HospitalizationId,
    int HeartRate,
    int SystolicBp,
    int DiastolicBp,
    int SpO2,
    decimal Temperature,
    int RespiratoryRate,
    Guid? RecordedByProfessionalId,
    string? Notes);

public record RecordVitalSignCorrectionRequest(
    Guid OriginalRecordId,
    int HeartRate,
    int SystolicBp,
    int DiastolicBp,
    int SpO2,
    decimal Temperature,
    int RespiratoryRate,
    Guid? RecordedByProfessionalId,
    string CorrectionReason);

public record IcuDashboardDto(
    int TotalIcuBeds,
    int OccupiedBeds,
    int CriticalAlerts,
    IReadOnlyList<IcuPatientDto> Patients);
