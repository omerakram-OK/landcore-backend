using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationSendResultDto> SendInstallmentDueReminderAsync(string adminId, string installmentPlanId, int seqNo, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationSendResultDto> SendOverdueWarningAsync(string adminId, string installmentPlanId, int seqNo, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationSendResultDto> SendRepossessionNoticeAsync(string adminId, string plotId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationSendResultDto> SendReceiptCopyAsync(string adminId, string receiptId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationSendResultDto> SendBouncedChequeAlertAsync(string adminId, string chequeId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationSendResultDto> SendSubscriptionAlertAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationScanResultDto> ScanAndSendInstallmentDueRemindersAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<NotificationScanResultDto> ScanAndSendSubscriptionAlertsAsync(string performedByUserId, CancellationToken cancellationToken = default);
}
