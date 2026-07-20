using Landcore.API.Middleware;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class BlocksController : ControllerBase
{
    private readonly IBlockService _blockService;

    public BlocksController(IBlockService blockService)
    {
        _blockService = blockService;
    }

    [HttpPost]
    [RequirePermission("Blocks", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateBlockRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _blockService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Blocks", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _blockService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("import")]
    [RequirePermission("Blocks", "Create")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> BulkImport(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationAppException(
                "A CSV or TXT file is required.",
                new Dictionary<string, string[]> { ["File"] = ["A CSV or TXT file is required."] });
        }

        string content;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        var result = await _blockService.BulkImportAsync(CurrentAdminId, content, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Blocks", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _blockService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    [RequirePermission("Blocks", "Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBlockRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _blockService.UpdateAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}")]
    [RequirePermission("Blocks", "Delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _blockService.DeleteAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(new { deleted = true }));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
