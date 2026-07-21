using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class BrandingController : ControllerBase
{
    private readonly IBrandingService _brandingService;

    public BrandingController(IBrandingService brandingService)
    {
        _brandingService = brandingService;
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
    {
        var result = await _brandingService.GetLogoAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("logo")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [RequestSizeLimit(1 * 1024 * 1024 + 4096)]
    public async Task<IActionResult> UploadLogo(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationAppException(
                "A logo image file is required.",
                new Dictionary<string, string[]> { ["file"] = ["A logo image file is required."] });
        }

        byte[] content;
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, cancellationToken);
            content = stream.ToArray();
        }

        var result = await _brandingService.UploadLogoAsync(CurrentAdminId, content, file.ContentType, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("logo")]
    [Authorize(Roles = Constants.Roles.Admin)]
    public async Task<IActionResult> RemoveLogo(CancellationToken cancellationToken)
    {
        var result = await _brandingService.RemoveLogoAsync(CurrentAdminId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
