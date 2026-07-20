using Landcore.Application.DTOs;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.SuperMan)]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.CreateAsync(request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.GetAllAsync(cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.GetByIdAsync(id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("by-admin/{adminId}")]
    public async Task<IActionResult> GetByAdminId(string adminId, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.GetByAdminIdAsync(adminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.UpdateAsync(id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.ActivateAsync(id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(string id, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.SuspendAsync(id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(string id, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.ReactivateAsync(id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private static object Envelope(object data) => new { success = true, data };
}
