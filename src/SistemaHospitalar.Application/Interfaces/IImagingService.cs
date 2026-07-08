using SistemaHospitalar.Application.DTOs.Imaging;

namespace SistemaHospitalar.Application.Interfaces;

public interface IImagingService
{
    Task<IReadOnlyList<ImagingStudyDto>> GetStudiesAsync(CancellationToken cancellationToken = default);
    Task<ImagingStudyDto> CreateStudyAsync(CreateImagingStudyRequest request, CancellationToken cancellationToken = default);
    Task<ImagingStudyDto?> UpdateStatusAsync(Guid id, UpdateImagingStudyStatusRequest request, CancellationToken cancellationToken = default);
    Task<ImagingStudyDto?> RegisterReportAsync(Guid id, RegisterImagingReportRequest request, CancellationToken cancellationToken = default);
}
