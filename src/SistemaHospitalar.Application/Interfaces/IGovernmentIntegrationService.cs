using SistemaHospitalar.Application.DTOs.Government;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IGovernmentIntegrationService
{
    IReadOnlyList<GovIntegrationProfileDto> GetProfiles();
    Task<CnsLookupResultDto> LookupCnsAsync(string cns, CancellationToken cancellationToken = default);
    Task<CnesEstablishmentDto> LookupCnesEstablishmentAsync(string cnesCode, CancellationToken cancellationToken = default);
    Task<GovIntegrationActionResultDto> ApplyCnsToPatientAsync(
        Guid patientId, ApplyCnsToPatientRequest request, CancellationToken cancellationToken = default);
    Task<SihAihPreviewDto?> GenerateSihAihPreviewAsync(Guid hospitalizationId, CancellationToken cancellationToken = default);
    Task<SiaDocumentPreviewDto> GenerateSiaPreviewAsync(
        SiaDocumentType documentType, string competence, CancellationToken cancellationToken = default);
    Task<DatasusExportFileDto> ExportSiaDocumentAsync(
        SiaDocumentType documentType, string competence, CancellationToken cancellationToken = default);
    Task<DatasusExportFileDto> ExportSihAihBatchAsync(
        string competence, CancellationToken cancellationToken = default);
    Task<DatasusExportFileDto> ExportCihaDocumentAsync(
        string competence, CancellationToken cancellationToken = default);
    Task<RndsPatientSummaryDto?> QueryRndsPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
}
