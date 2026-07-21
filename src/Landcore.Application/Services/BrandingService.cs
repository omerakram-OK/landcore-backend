using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class BrandingService : IBrandingService
{
    private const int MaxLogoBytes = 1 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/svg+xml",
    };

    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogger _auditLogger;

    public BrandingService(IAdminRepository adminRepository, IAuditLogger auditLogger)
    {
        _adminRepository = adminRepository;
        _auditLogger = auditLogger;
    }

    public async Task<BrandingResponseDto> GetLogoAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var admin = await LoadAdminOrThrowAsync(adminId, cancellationToken);
        return MapToDto(admin.LogoBase64, admin.LogoContentType);
    }

    public async Task<BrandingResponseDto> UploadLogoAsync(string adminId, byte[] fileContent, string contentType, string performedByUserId, CancellationToken cancellationToken = default)
    {
        if (fileContent.Length == 0)
        {
            throw new ValidationAppException(
                "The uploaded file is empty.",
                new Dictionary<string, string[]> { ["file"] = ["The uploaded file is empty."] });
        }

        if (fileContent.Length > MaxLogoBytes)
        {
            throw new ValidationAppException(
                "The logo file must be 1 MB or smaller.",
                new Dictionary<string, string[]> { ["file"] = ["The logo file must be 1 MB or smaller."] });
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationAppException(
                "The logo must be a PNG, JPEG, WEBP, or SVG image.",
                new Dictionary<string, string[]> { ["file"] = ["The logo must be a PNG, JPEG, WEBP, or SVG image."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var logoBase64 = Convert.ToBase64String(fileContent);

        await _adminRepository.SetLogoAsync(adminObjectId, logoBase64, contentType, performedBy, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LogoUploaded", "Admin", adminId, adminId);

        return MapToDto(logoBase64, contentType);
    }

    public async Task<BrandingResponseDto> RemoveLogoAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _adminRepository.SetLogoAsync(adminObjectId, null, null, performedBy, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LogoRemoved", "Admin", adminId, adminId);

        return MapToDto(null, null);
    }

    private async Task<Domain.Entities.Admin> LoadAdminOrThrowAsync(string adminId, CancellationToken cancellationToken)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var admin = await _adminRepository.GetByIdAsync(adminObjectId, cancellationToken);
        if (admin is null)
        {
            throw new NotFoundAppException($"Admin '{adminId}' was not found.");
        }

        return admin;
    }

    private static ObjectId ParseObjectId(string value, string fieldName)
    {
        if (!ObjectId.TryParse(value, out var id))
        {
            throw new ValidationAppException(
                $"'{fieldName}' is not a valid identifier.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid identifier."] });
        }

        return id;
    }

    private static BrandingResponseDto MapToDto(string? logoBase64, string? logoContentType)
    {
        if (string.IsNullOrEmpty(logoBase64) || string.IsNullOrEmpty(logoContentType))
        {
            return new BrandingResponseDto(null);
        }

        return new BrandingResponseDto($"data:{logoContentType};base64,{logoBase64}");
    }
}
