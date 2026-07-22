using System.Text.Json;
using Landcore.API.Middleware;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Landcore.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class InstallmentsController : ControllerBase
{
    private readonly IInstallmentPlanService _planService;
    private readonly IApprovalService _approvalService;

    public InstallmentsController(IInstallmentPlanService planService, IApprovalService approvalService)
    {
        _planService = planService;
        _approvalService = approvalService;
    }

    [HttpPost]
    [RequirePermission("Installments", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateInstallmentPlanRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _planService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Installments", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _planService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Installments", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _planService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("booking/{bookingId}")]
    [RequirePermission("Installments", "View")]
    public async Task<IActionResult> GetByBookingId(string bookingId, CancellationToken cancellationToken)
    {
        var result = await _planService.GetByBookingIdAsync(CurrentAdminId, bookingId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/discount")]
    [RequirePermission("Installments", "Edit")]
    public async Task<IActionResult> ApplyDiscount(string id, [FromBody] ApplyDiscountRequestDto request, CancellationToken cancellationToken)
    {
        if (User.IsInRole(Constants.Roles.Admin))
        {
            var result = await _planService.ApplyDiscountAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
            return Ok(Envelope(result));
        }

        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new ValidationAppException(
                "Justification is required when an Employee proposes a discount.",
                new Dictionary<string, string[]> { ["Justification"] = ["Justification is required when an Employee proposes a discount."] });
        }

        var payload = JsonSerializer.Serialize(new { PlanId = id, Request = request });
        var proposal = await _approvalService.ProposeAsync(CurrentAdminId, CurrentUserId, ApprovalRequestType.LargeDiscount, null, request.Justification, payload, cancellationToken);
        return Ok(Envelope(proposal));
    }

    [HttpPut("{id}/schedule")]
    [RequirePermission("Installments", "Edit")]
    public async Task<IActionResult> UpdateSchedule(string id, [FromBody] UpdateInstallmentPlanRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _planService.UpdateScheduleAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
