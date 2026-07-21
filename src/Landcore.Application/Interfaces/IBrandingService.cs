using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IBrandingService
{
    Task<BrandingResponseDto> GetLogoAsync(string adminId, CancellationToken cancellationToken = default);

    Task<BrandingResponseDto> UploadLogoAsync(string adminId, byte[] fileContent, string contentType, string performedByUserId, CancellationToken cancellationToken = default);

    Task<BrandingResponseDto> RemoveLogoAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default);
}
