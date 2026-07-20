using Landcore.Application.Configuration;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IInstallmentPlanRepository _planRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IChequeRepository _chequeRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;
    private readonly NotificationSettings _settings;

    public NotificationService(
        IInstallmentPlanRepository planRepository,
        IBookingRepository bookingRepository,
        IClientRepository clientRepository,
        IPlotRepository plotRepository,
        IReceiptRepository receiptRepository,
        IPaymentRepository paymentRepository,
        IChequeRepository chequeRepository,
        IAdminRepository adminRepository,
        ISubscriptionRepository subscriptionRepository,
        IEmailService emailService,
        IAuditLogger auditLogger,
        NotificationSettings settings)
    {
        _planRepository = planRepository;
        _bookingRepository = bookingRepository;
        _clientRepository = clientRepository;
        _plotRepository = plotRepository;
        _receiptRepository = receiptRepository;
        _paymentRepository = paymentRepository;
        _chequeRepository = chequeRepository;
        _adminRepository = adminRepository;
        _subscriptionRepository = subscriptionRepository;
        _emailService = emailService;
        _auditLogger = auditLogger;
        _settings = settings;
    }

    public async Task<NotificationSendResultDto> SendInstallmentDueReminderAsync(string adminId, string installmentPlanId, int seqNo, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plan = await LoadPlanOrThrowAsync(adminObjectId, installmentPlanId, cancellationToken);
        var installment = FindInstallmentOrThrow(plan, seqNo);
        var (client, _) = await LoadClientAndPlotForPlanAsync(adminObjectId, plan, cancellationToken);

        var subject = $"Upcoming installment due: #{installment.SeqNo}";
        var body =
            $"Dear {client.FullName},\n\n" +
            $"This is a reminder that Installment #{installment.SeqNo} of amount {(decimal)installment.Amount:0.00} " +
            $"is due on {installment.DueDate:yyyy-MM-dd}. Please arrange payment before the due date to avoid any " +
            "late charges.\n\nThank you.";

        return await SendAndLogAsync(
            "InstallmentDueReminder", client.Email, subject, body,
            performedByUserId, adminId, "InstallmentPlan", plan.Id.ToString(), cancellationToken);
    }

    public async Task<NotificationSendResultDto> SendOverdueWarningAsync(string adminId, string installmentPlanId, int seqNo, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plan = await LoadPlanOrThrowAsync(adminObjectId, installmentPlanId, cancellationToken);
        var installment = FindInstallmentOrThrow(plan, seqNo);
        var (client, _) = await LoadClientAndPlotForPlanAsync(adminObjectId, plan, cancellationToken);

        var outstanding = (decimal)installment.Amount - (decimal)installment.PaidAmount;
        var subject = $"Overdue installment: #{installment.SeqNo}";
        var body =
            $"Dear {client.FullName},\n\n" +
            $"Installment #{installment.SeqNo} (due {installment.DueDate:yyyy-MM-dd}) is now overdue, with " +
            $"{outstanding:0.00} still outstanding. Please settle this amount as soon as possible to avoid " +
            "further action on your plot.\n\nThank you.";

        return await SendAndLogAsync(
            "OverdueWarning", client.Email, subject, body,
            performedByUserId, adminId, "InstallmentPlan", plan.Id.ToString(), cancellationToken);
    }

    public async Task<NotificationSendResultDto> SendRepossessionNoticeAsync(string adminId, string plotId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(plotId, "plotId");

        var plot = await _plotRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (plot is null)
        {
            throw new NotFoundAppException($"Plot '{plotId}' was not found.");
        }

        var booking = await _bookingRepository.GetMostRecentByPlotIdAsync(adminObjectId, plot.Id, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundAppException($"No Booking was found for Plot '{plotId}' to notify.");
        }

        var client = await _clientRepository.GetByIdAsync(adminObjectId, booking.ClientId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundAppException($"Client '{booking.ClientId}' was not found.");
        }

        var subject = $"Repossession notice: Plot {plot.PlotNumber}";
        var body =
            $"Dear {client.FullName},\n\n" +
            $"This is to notify you that Plot {plot.PlotNumber} has been repossessed due to outstanding, " +
            "unpaid installments. Please contact us immediately to discuss your account and any applicable " +
            "recovery options.\n\nThank you.";

        return await SendAndLogAsync(
            "RepossessionNotice", client.Email, subject, body,
            performedByUserId, adminId, "Plot", plot.Id.ToString(), cancellationToken);
    }

    public async Task<NotificationSendResultDto> SendReceiptCopyAsync(string adminId, string receiptId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(receiptId, "receiptId");

        var receipt = await _receiptRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (receipt is null)
        {
            throw new NotFoundAppException($"Receipt '{receiptId}' was not found.");
        }

        var payment = await _paymentRepository.GetByIdAsync(adminObjectId, receipt.PaymentId, cancellationToken);
        if (payment is null)
        {
            throw new NotFoundAppException($"Payment '{receipt.PaymentId}' was not found.");
        }

        var plan = await _planRepository.GetByIdAsync(adminObjectId, payment.InstallmentPlanId, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundAppException($"InstallmentPlan '{payment.InstallmentPlanId}' was not found.");
        }

        var (client, _) = await LoadClientAndPlotForPlanAsync(adminObjectId, plan, cancellationToken);

        var subject = $"Receipt {receipt.ReceiptNumber}";
        var body =
            $"Dear {client.FullName},\n\n" +
            $"Thank you for your payment. Receipt {receipt.ReceiptNumber} confirms payment of " +
            $"{(decimal)payment.Amount:0.00} on {payment.Date:yyyy-MM-dd} against Installment #{payment.InstallmentSeqNo}.\n\n" +
            "Please keep this receipt for your records.\n\nThank you.";

        return await SendAndLogAsync(
            "ReceiptCopy", client.Email, subject, body,
            performedByUserId, adminId, "Receipt", receipt.Id.ToString(), cancellationToken);
    }

    public async Task<NotificationSendResultDto> SendBouncedChequeAlertAsync(string adminId, string chequeId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(chequeId, "chequeId");

        var cheque = await _chequeRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (cheque is null)
        {
            throw new NotFoundAppException($"Cheque '{chequeId}' was not found.");
        }

        var payment = await _paymentRepository.GetByIdAsync(adminObjectId, cheque.PaymentId, cancellationToken);
        if (payment is null)
        {
            throw new NotFoundAppException($"Payment '{cheque.PaymentId}' was not found.");
        }

        var plan = await _planRepository.GetByIdAsync(adminObjectId, payment.InstallmentPlanId, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundAppException($"InstallmentPlan '{payment.InstallmentPlanId}' was not found.");
        }

        var (client, _) = await LoadClientAndPlotForPlanAsync(adminObjectId, plan, cancellationToken);

        var penaltyText = cheque.BouncePenaltyAmount is null
            ? string.Empty
            : $" A penalty of {(decimal)cheque.BouncePenaltyAmount.Value:0.00} has been applied.";

        var subject = $"Cheque bounced: {cheque.ChequeNumber}";
        var body =
            $"Dear {client.FullName},\n\n" +
            $"Cheque {cheque.ChequeNumber} ({cheque.Bank}) for {(decimal)cheque.Amount:0.00} has bounced and the " +
            $"related installment has reverted to unpaid.{penaltyText} Please arrange an alternative payment as " +
            "soon as possible.\n\nThank you.";

        return await SendAndLogAsync(
            "BouncedChequeAlert", client.Email, subject, body,
            performedByUserId, adminId, "Cheque", cheque.Id.ToString(), cancellationToken);
    }

    public async Task<NotificationSendResultDto> SendSubscriptionAlertAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");

        var admin = await _adminRepository.GetByIdAsync(adminObjectId, cancellationToken);
        if (admin is null)
        {
            throw new NotFoundAppException($"Admin '{adminId}' was not found.");
        }

        var subscription = await _subscriptionRepository.GetByAdminIdAsync(adminObjectId, cancellationToken);
        if (subscription is null)
        {
            throw new NotFoundAppException($"No Subscription was found for Admin '{adminId}'.");
        }

        var isOverdue = subscription.Status == SubscriptionStatus.Overdue || subscription.NextDueDate < DateTime.UtcNow;
        var subject = isOverdue
            ? $"Subscription overdue: {admin.SocietyName}"
            : $"Subscription renewal due soon: {admin.SocietyName}";
        var body = isOverdue
            ? $"Dear {admin.SocietyName},\n\nYour {subscription.Plan} subscription (fee {(decimal)subscription.FeeAmount:0.00}) " +
              $"was due on {subscription.NextDueDate:yyyy-MM-dd} and is now overdue. Please renew promptly to avoid " +
              "service suspension.\n\nThank you."
            : $"Dear {admin.SocietyName},\n\nYour {subscription.Plan} subscription (fee {(decimal)subscription.FeeAmount:0.00}) " +
              $"is due for renewal on {subscription.NextDueDate:yyyy-MM-dd}. Please renew before this date to avoid " +
              "any interruption.\n\nThank you.";

        return await SendAndLogAsync(
            "SubscriptionAlert", admin.ContactEmail, subject, body,
            performedByUserId, null, "Subscription", subscription.Id.ToString(), cancellationToken);
    }

    public async Task<NotificationScanResultDto> ScanAndSendInstallmentDueRemindersAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(_settings.InstallmentDueReminderDays);

        var plans = await _planRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        var sent = 0;
        var failed = 0;
        var details = new List<string>();

        foreach (var plan in plans)
        {
            foreach (var installment in plan.Installments)
            {
                NotificationSendResultDto? result = null;

                if (installment.Status == InstallmentStatus.Late)
                {
                    result = await SendOverdueWarningAsync(adminId, plan.Id.ToString(), installment.SeqNo, performedByUserId, cancellationToken);
                }
                else if (installment.Status is InstallmentStatus.Pending or InstallmentStatus.PartiallyPaid
                    && installment.DueDate >= now && installment.DueDate <= horizon)
                {
                    result = await SendInstallmentDueReminderAsync(adminId, plan.Id.ToString(), installment.SeqNo, performedByUserId, cancellationToken);
                }

                if (result is null)
                {
                    continue;
                }

                if (result.Sent)
                {
                    sent++;
                }
                else
                {
                    failed++;
                }

                details.Add($"InstallmentPlan {plan.Id} #{installment.SeqNo} ({result.NotificationType}): {(result.Sent ? "sent" : $"failed - {result.FailureReason}")}");
            }
        }

        return new NotificationScanResultDto(sent, failed, details);
    }

    public async Task<NotificationScanResultDto> ScanAndSendSubscriptionAlertsAsync(string performedByUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(_settings.InstallmentDueReminderDays);

        var admins = await _adminRepository.GetAllAsync(cancellationToken);
        var sent = 0;
        var failed = 0;
        var details = new List<string>();

        foreach (var admin in admins)
        {
            var subscription = await _subscriptionRepository.GetByAdminIdAsync(admin.Id, cancellationToken);
            if (subscription is null)
            {
                continue;
            }

            var isDueSoon = subscription.NextDueDate >= now && subscription.NextDueDate <= horizon;
            var isOverdue = subscription.Status == SubscriptionStatus.Overdue;
            if (!isDueSoon && !isOverdue)
            {
                continue;
            }

            var result = await SendSubscriptionAlertAsync(admin.Id.ToString(), performedByUserId, cancellationToken);
            if (result.Sent)
            {
                sent++;
            }
            else
            {
                failed++;
            }

            details.Add($"Admin {admin.Id} ({result.NotificationType}): {(result.Sent ? "sent" : $"failed - {result.FailureReason}")}");
        }

        return new NotificationScanResultDto(sent, failed, details);
    }

    private async Task<NotificationSendResultDto> SendAndLogAsync(
        string notificationType,
        string recipientEmail,
        string subject,
        string body,
        string performedByUserId,
        string? adminScope,
        string entity,
        string entityId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(recipientEmail, subject, body, isHtml: false, cancellationToken);
            _auditLogger.LogAction(performedByUserId, $"Notification{notificationType}Sent", entity, entityId, adminScope,
                new { RecipientEmail = recipientEmail, Subject = subject });
            return new NotificationSendResultDto(true, notificationType, recipientEmail, null);
        }
        catch (Exception ex)
        {
            _auditLogger.LogAction(performedByUserId, $"Notification{notificationType}Failed", entity, entityId, adminScope,
                new { RecipientEmail = recipientEmail, Subject = subject, Error = ex.Message });
            return new NotificationSendResultDto(false, notificationType, recipientEmail, ex.Message);
        }
    }

    private async Task<InstallmentPlan> LoadPlanOrThrowAsync(ObjectId adminObjectId, string installmentPlanId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(installmentPlanId, "installmentPlanId");
        var plan = await _planRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundAppException($"InstallmentPlan '{installmentPlanId}' was not found.");
        }

        return plan;
    }

    private static Installment FindInstallmentOrThrow(InstallmentPlan plan, int seqNo)
    {
        var installment = plan.Installments.FirstOrDefault(i => i.SeqNo == seqNo);
        if (installment is null)
        {
            throw new NotFoundAppException($"Installment #{seqNo} was not found on InstallmentPlan '{plan.Id}'.");
        }

        return installment;
    }

    private async Task<(Client Client, Plot Plot)> LoadClientAndPlotForPlanAsync(ObjectId adminObjectId, InstallmentPlan plan, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(adminObjectId, plan.BookingId, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundAppException($"Booking '{plan.BookingId}' was not found.");
        }

        var client = await _clientRepository.GetByIdAsync(adminObjectId, booking.ClientId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundAppException($"Client '{booking.ClientId}' was not found.");
        }

        var plot = await _plotRepository.GetByIdAsync(adminObjectId, booking.PlotId, cancellationToken);
        if (plot is null)
        {
            throw new NotFoundAppException($"Plot '{booking.PlotId}' was not found.");
        }

        return (client, plot);
    }

    private static ObjectId ParseObjectId(string value, string fieldName)
    {
        if (!ObjectId.TryParse(value, out var id))
        {
            throw new ValidationAppException(
                $"'{fieldName}' is not a valid identifier.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid identifier."] });
        }

        return id;
    }
}
