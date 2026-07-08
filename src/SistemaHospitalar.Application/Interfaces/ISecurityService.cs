using SistemaHospitalar.Application.DTOs.Security;

namespace SistemaHospitalar.Application.Interfaces;

public interface ISecurityService
{
    Task<SecurityDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityIncidentDto>> GetIncidentsAsync(CancellationToken cancellationToken = default);
    Task<SecurityIncidentDto> CreateIncidentAsync(CreateSecurityIncidentRequest request, CancellationToken cancellationToken = default);
    Task<SecurityIncidentDto?> ResolveIncidentAsync(Guid id, ResolveIncidentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VisitorLogDto>> GetVisitorsAsync(bool? insideOnly, CancellationToken cancellationToken = default);
    Task<VisitorLogDto> RegisterVisitorAsync(RegisterVisitorRequest request, CancellationToken cancellationToken = default);
    Task<VisitorLogDto?> RegisterExitAsync(Guid visitorLogId, CancellationToken cancellationToken = default);
    SecuritySettingsDto GetSettings();
}
