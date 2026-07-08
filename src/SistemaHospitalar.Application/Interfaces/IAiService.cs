using SistemaHospitalar.Application.DTOs.Ai;
using SistemaHospitalar.Application.DTOs.ClinicalIntelligence;

namespace SistemaHospitalar.Application.Interfaces;

public interface IAiService
{
    Task<TriageResponse> AnalyzeTriageAsync(TriageRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cid10SuggestionDto>> SuggestCid10Async(Cid10SuggestionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiTriageLogDto>> GetRecentTriageLogsAsync(int limit, CancellationToken cancellationToken = default);
    Task<TriageAdmissionSuggestionDto?> GetAdmissionSuggestionForPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default);
    Task<AiInsightReportDto> AnalyzeOutbreakAsync(int days = 30, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<AiInsightReportDto> AnalyzeRecurrentPatientAsync(Guid patientId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<AiInsightReportDto> AnalyzeTriageOperationalAsync(int days = 7, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiInsightReportDto>> GetInsightReportsAsync(int limit = 20, AiInsightType? type = null, CancellationToken cancellationToken = default);
    Task<AiInsightReportDto?> GetInsightReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PrescriptionSafetyResultDto> AnalyzePrescriptionSafetyAsync(
        Guid patientId,
        string prescriptionContent,
        CancellationToken cancellationToken = default);
    Task<AiInsightReportDto> AnalyzeHospitalDashboardAsync(
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
