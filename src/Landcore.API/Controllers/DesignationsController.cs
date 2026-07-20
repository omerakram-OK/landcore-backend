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
public class DesignationsController : ControllerBase
{
    private readonly IDesignationService _designationService;

    public DesignationsController(IDesignationService designationService)
    {
        _designationService = designationService;
    }

    [HttpPost]
    [RequirePermission("Designations", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _designationService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Designations", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _designationService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Designations", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _designationService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    [RequirePermission("Designations", "Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDesignationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _designationService.UpdateAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}")]
    [RequirePermission("Designations", "Delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _designationService.DeleteAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(new { deleted = true }));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
