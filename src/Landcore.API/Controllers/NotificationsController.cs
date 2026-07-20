using Landcore.API.Middleware;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("scan-due-installments")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Notifications", "Trigger")]
    public async Task<IActionResult> ScanDueInstallments(CancellationToken cancellationToken)
    {
        var result = await _notificationService.ScanAndSendInstallmentDueRemindersAsync(CurrentAdminId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("installment-due-reminder/{installmentPlanId}/{seqNo:int}")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Notifications", "Trigger")]
    public async Task<IActionResult> SendInstallmentDueReminder(string installmentPlanId, int seqNo, CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendInstallmentDueReminderAsync(CurrentAdminId, installmentPlanId, seqNo, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("overdue-warning/{installmentPlanId}/{seqNo:int}")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Notifications", "Trigger")]
    public async Task<IActionResult> SendOverdueWarning(string installmentPlanId, int seqNo, CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendOverdueWarningAsync(CurrentAdminId, installmentPlanId, seqNo, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("repossession-notice/{plotId}")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Notifications", "Trigger")]
    public async Task<IActionResult> SendRepossessionNotice(string plotId, CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendRepossessionNoticeAsync(CurrentAdminId, plotId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("receipt-copy/{receiptId}")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Notifications", "Trigger")]
    public async Task<IActionResult> SendReceiptCopy(string receiptId, CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendReceiptCopyAsync(CurrentAdminId, receiptId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("bounced-cheque-alert/{chequeId}")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Notifications", "Trigger")]
    public async Task<IActionResult> SendBouncedChequeAlert(string chequeId, CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendBouncedChequeAlertAsync(CurrentAdminId, chequeId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("scan-subscriptions")]
    [Authorize(Roles = Constants.Roles.SuperMan)]
    public async Task<IActionResult> ScanSubscriptions(CancellationToken cancellationToken)
    {
        var result = await _notificationService.ScanAndSendSubscriptionAlertsAsync(CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPost("subscription-alert/{adminId}")]
    [Authorize(Roles = Constants.Roles.SuperMan)]
    public async Task<IActionResult> SendSubscriptionAlert(string adminId, CancellationToken cancellationToken)
    {
        var result = await _notificationService.SendSubscriptionAlertAsync(adminId, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
