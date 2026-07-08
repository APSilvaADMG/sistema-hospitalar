using SistemaHospitalar.Application.DTOs.ClinicalIntelligence;

namespace SistemaHospitalar.Application.Interfaces;

public interface IClinicalIntelligenceService
{
    Task<PatientClinicalAlertsDto?> GetPatientClinicalAlertsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockReplenishmentSuggestionDto>> GetStockReplenishmentSuggestionsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalInsightsDto> GetOperationalInsightsAsync(
        CancellationToken cancellationToken = default);
}
