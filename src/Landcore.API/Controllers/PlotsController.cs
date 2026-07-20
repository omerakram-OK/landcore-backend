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
public class PlotsController : ControllerBase
{
    private readonly IPlotService _plotService;
    private readonly IRepossessionService _repossessionService;
    private readonly IApprovalService _approvalService;

    public PlotsController(IPlotService plotService, IRepossessionService repossessionService, IApprovalService approvalService)
    {
        _plotService = plotService;
        _repossessionService = repossessionService;
        _approvalService = approvalService;
    }

    [HttpPost]
    [RequirePermission("Plots", "Create")]
    public async Task<IActionResult> Create([FromBody] CreatePlotRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plotService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Plots", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _plotService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("import")]
    [RequirePermission("Plots", "Create")]
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

        var result = await _plotService.BulkImportAsync(CurrentAdminId, content, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Plots", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _plotService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePlotRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plotService.UpdateAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}")]
    [RequirePermission("Plots", "Delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _plotService.DeleteAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(new { deleted = true }));
    }

    [HttpPost("{id}/charges")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> AddOrUpdateCharge(string id, [FromBody] AddOrUpdatePlotChargeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plotService.AddOrUpdateChargeAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}/charges/{chargeType}")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> RemoveCharge(string id, string chargeType, CancellationToken cancellationToken)
    {
        var result = await _plotService.RemoveChargeAsync(CurrentAdminId, id, chargeType, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/maintenance-charge")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> SetAnnualMaintenanceCharge(string id, [FromBody] SetAnnualMaintenanceChargeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plotService.SetAnnualMaintenanceChargeAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/status")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangePlotStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plotService.ChangeStatusAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}/possession-status")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> UpdatePossessionStatus(string id, [FromBody] UpdatePlotPossessionStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plotService.UpdatePossessionStatusAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/split")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> Split(string id, [FromBody] SplitPlotRequestDto request, CancellationToken cancellationToken)
    {
        if (User.IsInRole(Constants.Roles.Admin))
        {
            var result = await _plotService.SplitAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
            return Ok(Envelope(result));
        }

        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new ValidationAppException(
                "Justification is required when an Employee proposes a split.",
                new Dictionary<string, string[]> { ["Justification"] = ["Justification is required when an Employee proposes a split."] });
        }

        var payload = JsonSerializer.Serialize(new { Operation = "Split", PlotId = id, Request = request });
        var proposal = await _approvalService.ProposeAsync(CurrentAdminId, CurrentUserId, ApprovalRequestType.MergeSplit, id, request.Justification, payload, cancellationToken);
        return Ok(Envelope(proposal));
    }

    [HttpPost("merge")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> Merge([FromBody] MergePlotsRequestDto request, CancellationToken cancellationToken)
    {
        if (User.IsInRole(Constants.Roles.Admin))
        {
            var result = await _plotService.MergeAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
            return Ok(Envelope(result));
        }

        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new ValidationAppException(
                "Justification is required when an Employee proposes a merge.",
                new Dictionary<string, string[]> { ["Justification"] = ["Justification is required when an Employee proposes a merge."] });
        }

        var payload = JsonSerializer.Serialize(new { Operation = "Merge", Request = request });
        var proposal = await _approvalService.ProposeAsync(CurrentAdminId, CurrentUserId, ApprovalRequestType.MergeSplit, null, request.Justification, payload, cancellationToken);
        return Ok(Envelope(proposal));
    }

    [HttpPut("repossession-scan")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> RepossessionScan(CancellationToken cancellationToken)
    {
        var result = await _repossessionService.ScanAndFlagOverdueAsync(CurrentAdminId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("{id}/late-payment")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> RecordLatePayment(string id, [FromBody] RecordLatePaymentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _repossessionService.RecordLatePaymentAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("refunds")]
    [RequirePermission("Plots", "View")]
    public async Task<IActionResult> GetAllRefunds(CancellationToken cancellationToken)
    {
        var result = await _repossessionService.GetAllRefundsAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}/refunds")]
    [RequirePermission("Plots", "View")]
    public async Task<IActionResult> GetRefundsByPlotId(string id, CancellationToken cancellationToken)
    {
        var result = await _repossessionService.GetRefundsByPlotIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("refunds/{refundRecordId}")]
    [RequirePermission("Plots", "View")]
    public async Task<IActionResult> GetRefundById(string refundRecordId, CancellationToken cancellationToken)
    {
        var result = await _repossessionService.GetRefundByIdAsync(CurrentAdminId, refundRecordId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("refunds/{refundRecordId}/issue")]
    [RequirePermission("Plots", "Edit")]
    public async Task<IActionResult> IssueRefund(string refundRecordId, [FromBody] IssueRefundRequestDto request, CancellationToken cancellationToken)
    {
        if (User.IsInRole(Constants.Roles.Admin))
        {
            var result = await _repossessionService.IssueRefundAsync(CurrentAdminId, refundRecordId, CurrentUserId, cancellationToken);
            return Ok(Envelope(result));
        }

        if (string.IsNullOrWhiteSpace(request?.Justification))
        {
            throw new ValidationAppException(
                "Justification is required when an Employee proposes a refund issuance.",
                new Dictionary<string, string[]> { ["Justification"] = ["Justification is required when an Employee proposes a refund issuance."] });
        }

        var refund = await _repossessionService.GetRefundByIdAsync(CurrentAdminId, refundRecordId, cancellationToken);
        var payload = JsonSerializer.Serialize(new { RefundRecordId = refundRecordId });
        var proposal = await _approvalService.ProposeAsync(CurrentAdminId, CurrentUserId, ApprovalRequestType.Refund, refund.PlotId, request.Justification, payload, cancellationToken);
        return Ok(Envelope(proposal));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
