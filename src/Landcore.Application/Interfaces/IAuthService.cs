using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
