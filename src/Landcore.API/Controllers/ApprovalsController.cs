using FluentValidation;
using Landcore.API.Middleware;
using Landcore.Application.DTOs;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Common;
using Landcore.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly IValidator<ProposeApprovalRequestDto> _proposeValidator;

    public ApprovalsController(IApprovalService approvalService, IValidator<ProposeApprovalRequestDto> proposeValidator)
    {
        _approvalService = approvalService;
        _proposeValidator = proposeValidator;
    }

    [HttpPost]
    [RequirePermission("Approvals", "Propose")]
    public async Task<IActionResult> Propose([FromBody] ProposeApprovalRequestDto request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateOrThrowAsync(_proposeValidator, request, cancellationToken);

        Enum.TryParse<ApprovalRequestType>(request.Type, ignoreCase: true, out var type);

        var result = await _approvalService.ProposeAsync(CurrentAdminId, CurrentUserId, type, request.TargetPlotId, request.Justification, request.PayloadJson, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Approvals", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _approvalService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Approvals", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _approvalService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [RequirePermission("Approvals", "Decide")]
    public async Task<IActionResult> Approve(string id, [FromBody] ApproveApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _approvalService.ApproveAsync(CurrentAdminId, id, CurrentUserId, request.DecisionNotes, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [RequirePermission("Approvals", "Decide")]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectApprovalRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _approvalService.RejectAsync(CurrentAdminId, id, CurrentUserId, request.DecisionNotes, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
