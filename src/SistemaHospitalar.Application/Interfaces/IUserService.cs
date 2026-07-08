using SistemaHospitalar.Application.DTOs.Auth;

namespace SistemaHospitalar.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserListDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailDto?> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
}
