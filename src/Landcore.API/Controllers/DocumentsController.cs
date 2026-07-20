using Landcore.API.Middleware;
using Landcore.Application.DTOs;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentGenerationService _documentGenerationService;

    public DocumentsController(IDocumentGenerationService documentGenerationService)
    {
        _documentGenerationService = documentGenerationService;
    }

    [HttpPost("generate")]
    [RequirePermission("Documents", "Generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _documentGenerationService.GenerateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("/api/plots/{plotId}/documents")]
    [RequirePermission("Documents", "View")]
    public async Task<IActionResult> GetHistoryByPlotId(string plotId, CancellationToken cancellationToken)
    {
        var result = await _documentGenerationService.GetHistoryByPlotIdAsync(CurrentAdminId, plotId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}/download")]
    [RequirePermission("Documents", "View")]
    public async Task<IActionResult> Download(string id, CancellationToken cancellationToken)
    {
        var file = await _documentGenerationService.DownloadAsync(CurrentAdminId, id, cancellationToken);
        return File(file.FileContent, "application/pdf", file.FileName);
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
