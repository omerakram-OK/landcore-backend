using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInstallmentPlanRepository _planRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IChequeRepository _chequeRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IValidator<RecordPaymentRequestDto> _recordValidator;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificationService _notificationService;

    private const string ReceiptPrefix = "RCPT-";

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInstallmentPlanRepository planRepository,
        IBookingRepository bookingRepository,
        IPlotRepository plotRepository,
        IReceiptRepository receiptRepository,
        IChequeRepository chequeRepository,
        IBankAccountRepository bankAccountRepository,
        IValidator<RecordPaymentRequestDto> recordValidator,
        IAuditLogger auditLogger,
        INotificationService notificationService)
    {
        _paymentRepository = paymentRepository;
        _planRepository = planRepository;
        _bookingRepository = bookingRepository;
        _plotRepository = plotRepository;
        _receiptRepository = receiptRepository;
        _chequeRepository = chequeRepository;
        _bankAccountRepository = bankAccountRepository;
        _recordValidator = recordValidator;
        _auditLogger = auditLogger;
        _notificationService = notificationService;
    }

    public async Task<PaymentResponseDto> RecordPaymentAsync(string adminId, RecordPaymentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_recordValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var planId = ParseObjectId(request.InstallmentPlanId, "InstallmentPlanId");

        var plan = await _planRepository.GetByIdAsync(adminObjectId, planId, cancellationToken);
        if (plan is null)
        {
            throw new ValidationAppException(
                "The InstallmentPlan was not found for this Admin.",
                new Dictionary<string, string[]> { ["InstallmentPlanId"] = ["The InstallmentPlan was not found for this Admin."] });
        }

        var installment = plan.Installments.FirstOrDefault(i => i.SeqNo == request.InstallmentSeqNo);
        if (installment is null)
        {
            throw new ValidationAppException(
                $"Installment #{request.InstallmentSeqNo} was not found on this plan.",
                new Dictionary<string, string[]> { ["InstallmentSeqNo"] = [$"Installment #{request.InstallmentSeqNo} was not found on this plan."] });
        }

        if (installment.Status == InstallmentStatus.Paid)
        {
            throw new ValidationAppException(
                $"Installment #{request.InstallmentSeqNo} is already fully paid.",
                new Dictionary<string, string[]> { ["InstallmentSeqNo"] = [$"Installment #{request.InstallmentSeqNo} is already fully paid."] });
        }

        var mode = Enum.Parse<PaymentMode>(request.Mode, ignoreCase: true);

        ObjectId? bankAccountId = null;
        if (!string.IsNullOrWhiteSpace(request.BankAccountId))
        {
            bankAccountId = ParseObjectId(request.BankAccountId, "BankAccountId");

            var bankAccount = await _bankAccountRepository.GetByIdAsync(adminObjectId, bankAccountId.Value, cancellationToken);
            if (bankAccount is null)
            {
                throw new ValidationAppException(
                    "The BankAccount was not found for this Admin.",
                    new Dictionary<string, string[]> { ["BankAccountId"] = ["The BankAccount was not found for this Admin."] });
            }
        }

        var preCredit = (decimal)plan.CreditBalance;
        var outstanding = Math.Max(0m, (decimal)installment.Amount - (decimal)installment.PaidAmount);
        var pool = request.Amount + preCredit;
        var appliedToInstallment = Math.Min(pool, outstanding);
        var creditConsumed = Math.Min(preCredit, appliedToInstallment);
        var newCreditBalance = pool - appliedToInstallment;

        installment.PaidAmount = (Decimal128)((decimal)installment.PaidAmount + appliedToInstallment);
        installment.Status = (decimal)installment.PaidAmount >= (decimal)installment.Amount
            ? InstallmentStatus.Paid
            : InstallmentStatus.PartiallyPaid;

        plan.CreditBalance = (Decimal128)newCreditBalance;

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        plan.UpdatedAt = now;
        plan.UpdatedBy = performedBy;
        await _planRepository.UpdateAsync(plan, cancellationToken);

        var receiptCount = await _receiptRepository.CountByAdminIdAsync(adminObjectId, cancellationToken);
        var receiptNumber = $"{ReceiptPrefix}{receiptCount + 1:D6}";

        var paymentId = ObjectId.GenerateNewId();
        var receiptId = ObjectId.GenerateNewId();

        var receipt = new Receipt
        {
            Id = receiptId,
            AdminId = adminObjectId,
            ReceiptNumber = receiptNumber,
            PaymentId = paymentId,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };
        await _receiptRepository.CreateAsync(receipt, cancellationToken);

        var payment = new Payment
        {
            Id = paymentId,
            AdminId = adminObjectId,
            InstallmentPlanId = planId,
            InstallmentSeqNo = request.InstallmentSeqNo,
            Amount = (Decimal128)request.Amount,
            Mode = mode,
            BankAccountId = bankAccountId,
            Date = request.Date,
            ReceiptId = receiptId,
            CreditBalanceApplied = creditConsumed > 0 ? (Decimal128)creditConsumed : null,
            AmountAppliedToInstallment = (Decimal128)appliedToInstallment,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };
        await _paymentRepository.CreateAsync(payment, cancellationToken);

        if (mode == PaymentMode.Cheque)
        {
            var cheque = new Cheque
            {
                Id = ObjectId.GenerateNewId(),
                AdminId = adminObjectId,
                PaymentId = paymentId,
                ChequeNumber = request.ChequeNumber!,
                Bank = request.ChequeBank!,
                Amount = (Decimal128)request.Amount,
                DueDate = request.ChequeDueDate!.Value,
                DepositDate = request.ChequeDepositDate!.Value,
                Status = ChequeStatus.Pending,
                BouncePenaltyAmount = null,
                CreatedAt = now,
                CreatedBy = performedBy,
                UpdatedAt = now,
                UpdatedBy = performedBy,
                IsDeleted = false,
            };
            await _chequeRepository.CreateAsync(cheque, cancellationToken);
        }

        if (plan.Installments.All(i => i.Status == InstallmentStatus.Paid))
        {
            var booking = await _bookingRepository.GetByIdAsync(adminObjectId, plan.BookingId, cancellationToken);
            if (booking is not null)
            {
                var plot = await _plotRepository.GetByIdAsync(adminObjectId, booking.PlotId, cancellationToken);
                if (plot is not null && plot.Status == PlotStatus.Booked)
                {
                    plot.Status = PlotStatus.Sold;
                    plot.HistoryLog.Add(new Plot.HistoryLogEntry
                    {
                        Event = "StatusChanged",
                        Details = $"Booked -> Sold: InstallmentPlan {plan.Id} fully paid.",
                        At = now,
                        By = performedBy,
                    });
                    plot.UpdatedAt = now;
                    plot.UpdatedBy = performedBy;
                    await _plotRepository.UpdateAsync(plot, cancellationToken);
                }
            }
        }

        _auditLogger.LogAction(performedByUserId, "PaymentRecorded", "Payment", payment.Id.ToString(), adminId, new
        {
            InstallmentPlanId = planId.ToString(),
            InstallmentSeqNo = request.InstallmentSeqNo,
            Amount = request.Amount,
            Mode = mode.ToString(),
            ReceiptNumber = receiptNumber,
        });

        try
        {
            await _notificationService.SendReceiptCopyAsync(adminId, receiptId.ToString(), performedByUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _auditLogger.LogAction(performedByUserId, "ReceiptCopyNotificationError", "Receipt", receiptId.ToString(), adminId, new { Error = ex.Message });
        }

        return MapToDto(payment, receiptNumber);
    }

    public async Task<IReadOnlyList<PaymentResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var payments = await _paymentRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return await MapToDtosAsync(adminObjectId, payments, cancellationToken);
    }

    public async Task<PaymentResponseDto> GetByIdAsync(string adminId, string paymentId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(paymentId, "paymentId");
        var payment = await _paymentRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (payment is null)
        {
            throw new NotFoundAppException($"Payment '{paymentId}' was not found.");
        }

        var receipt = await _receiptRepository.GetByIdAsync(adminObjectId, payment.ReceiptId, cancellationToken);
        return MapToDto(payment, receipt?.ReceiptNumber);
    }

    public async Task<IReadOnlyList<PaymentResponseDto>> GetByInstallmentPlanIdAsync(string adminId, string installmentPlanId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var planId = ParseObjectId(installmentPlanId, "installmentPlanId");
        var payments = await _paymentRepository.GetByInstallmentPlanIdAsync(adminObjectId, planId, cancellationToken);
        return await MapToDtosAsync(adminObjectId, payments, cancellationToken);
    }

    private async Task<IReadOnlyList<PaymentResponseDto>> MapToDtosAsync(ObjectId adminObjectId, IReadOnlyList<Payment> payments, CancellationToken cancellationToken)
    {
        var results = new List<PaymentResponseDto>(payments.Count);
        foreach (var payment in payments)
        {
            var receipt = await _receiptRepository.GetByIdAsync(adminObjectId, payment.ReceiptId, cancellationToken);
            results.Add(MapToDto(payment, receipt?.ReceiptNumber));
        }

        return results;
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

    private static PaymentResponseDto MapToDto(Payment payment, string? receiptNumber) => new(
        payment.Id.ToString(),
        payment.AdminId.ToString(),
        payment.InstallmentPlanId.ToString(),
        payment.InstallmentSeqNo,
        (decimal)payment.Amount,
        payment.Mode.ToString(),
        payment.BankAccountId?.ToString(),
        payment.Date,
        payment.ReceiptId.ToString(),
        receiptNumber,
        payment.CreditBalanceApplied is null ? null : (decimal)payment.CreditBalanceApplied.Value,
        payment.CreatedAt);
}
