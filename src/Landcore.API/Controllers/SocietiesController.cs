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
public class SocietiesController : ControllerBase
{
    private readonly ISocietyService _societyService;

    public SocietiesController(ISocietyService societyService)
    {
        _societyService = societyService;
    }

    [HttpPost]
    [Authorize(Roles = Constants.Roles.Admin)]
    [RequirePermission("Societies", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateSocietyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _societyService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Societies", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _societyService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Societies", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _societyService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    [RequirePermission("Societies", "Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSocietyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _societyService.UpdateAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}")]
    [RequirePermission("Societies", "Delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _societyService.DeleteAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(new { deleted = true }));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
