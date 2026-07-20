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
public class ChequesController : ControllerBase
{
    private readonly IChequeService _chequeService;

    public ChequesController(IChequeService chequeService)
    {
        _chequeService = chequeService;
    }

    [HttpGet]
    [RequirePermission("Cheques", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _chequeService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Cheques", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _chequeService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/clear")]
    [RequirePermission("Cheques", "Edit")]
    public async Task<IActionResult> Clear(string id, CancellationToken cancellationToken)
    {
        var result = await _chequeService.ClearAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/bounce")]
    [RequirePermission("Cheques", "Edit")]
    public async Task<IActionResult> Bounce(string id, [FromBody] BounceChequeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _chequeService.MarkBouncedAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
