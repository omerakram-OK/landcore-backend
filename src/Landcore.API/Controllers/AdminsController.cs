using Landcore.Application.DTOs;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.SuperMan)]
public class AdminsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateAsync(request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetAllAsync(cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetByIdAsync(id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAdminRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(string id, CancellationToken cancellationToken)
    {
        var result = await _adminService.SuspendAsync(id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(string id, CancellationToken cancellationToken)
    {
        var result = await _adminService.ReactivateAsync(id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private static object Envelope(object data) => new { success = true, data };
}
