using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class ChequeService : IChequeService
{
    private readonly IChequeRepository _chequeRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInstallmentPlanRepository _planRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IValidator<BounceChequeRequestDto> _bounceValidator;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificationService _notificationService;

    public ChequeService(
        IChequeRepository chequeRepository,
        IPaymentRepository paymentRepository,
        IInstallmentPlanRepository planRepository,
        IBookingRepository bookingRepository,
        IPlotRepository plotRepository,
        IValidator<BounceChequeRequestDto> bounceValidator,
        IAuditLogger auditLogger,
        INotificationService notificationService)
    {
        _chequeRepository = chequeRepository;
        _paymentRepository = paymentRepository;
        _planRepository = planRepository;
        _bookingRepository = bookingRepository;
        _plotRepository = plotRepository;
        _bounceValidator = bounceValidator;
        _auditLogger = auditLogger;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<ChequeResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var cheques = await _chequeRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return cheques.Select(MapToDto).ToList();
    }

    public async Task<ChequeResponseDto> GetByIdAsync(string adminId, string chequeId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var cheque = await LoadChequeOrThrowAsync(adminObjectId, chequeId, cancellationToken);
        return MapToDto(cheque);
    }

    public async Task<ChequeResponseDto> ClearAsync(string adminId, string chequeId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var cheque = await LoadChequeOrThrowAsync(adminObjectId, chequeId, cancellationToken);

        if (cheque.Status != ChequeStatus.Pending)
        {
            throw new ValidationAppException(
                $"Cheque cannot be cleared from status '{cheque.Status}' (only a Pending cheque can be cleared).",
                new Dictionary<string, string[]> { ["Status"] = [$"Cheque is '{cheque.Status}', not Pending."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        cheque.Status = ChequeStatus.Cleared;
        cheque.UpdatedAt = now;
        cheque.UpdatedBy = performedBy;
        await _chequeRepository.UpdateAsync(cheque, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "ChequeCleared", "Cheque", cheque.Id.ToString(), adminId);

        return MapToDto(cheque);
    }

    public async Task<ChequeResponseDto> MarkBouncedAsync(string adminId, string chequeId, BounceChequeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_bounceValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var cheque = await LoadChequeOrThrowAsync(adminObjectId, chequeId, cancellationToken);

        if (cheque.Status != ChequeStatus.Pending)
        {
            throw new ValidationAppException(
                $"Cheque cannot be bounced from status '{cheque.Status}' (only a Pending cheque can be bounced).",
                new Dictionary<string, string[]> { ["Status"] = [$"Cheque is '{cheque.Status}', not Pending."] });
        }

        var payment = await _paymentRepository.GetByIdAsync(adminObjectId, cheque.PaymentId, cancellationToken);
        if (payment is null)
        {
            throw new NotFoundAppException($"The Payment linked to Cheque '{chequeId}' was not found.");
        }

        var plan = await _planRepository.GetByIdAsync(adminObjectId, payment.InstallmentPlanId, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundAppException($"The InstallmentPlan linked to Cheque '{chequeId}' was not found.");
        }

        var installment = plan.Installments.FirstOrDefault(i => i.SeqNo == payment.InstallmentSeqNo);
        if (installment is null)
        {
            throw new NotFoundAppException($"Installment #{payment.InstallmentSeqNo} linked to Cheque '{chequeId}' was not found.");
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var amountToReverse = (decimal)payment.AmountAppliedToInstallment;
        installment.PaidAmount = (Decimal128)Math.Max(0m, (decimal)installment.PaidAmount - amountToReverse);
        installment.Status = InstallmentStatus.Pending;

        if (payment.CreditBalanceApplied is not null)
        {
            plan.CreditBalance = (Decimal128)((decimal)plan.CreditBalance + (decimal)payment.CreditBalanceApplied.Value);
        }

        plan.UpdatedAt = now;
        plan.UpdatedBy = performedBy;
        await _planRepository.UpdateAsync(plan, cancellationToken);

        var booking = await _bookingRepository.GetByIdAsync(adminObjectId, plan.BookingId, cancellationToken);
        if (booking is not null)
        {
            var plot = await _plotRepository.GetByIdAsync(adminObjectId, booking.PlotId, cancellationToken);
            if (plot is not null && plot.Status == PlotStatus.Sold)
            {
                plot.Status = PlotStatus.Booked;
                plot.HistoryLog.Add(new Plot.HistoryLogEntry
                {
                    Event = "StatusChanged",
                    Details = $"Sold -> Booked: Cheque {cheque.Id} bounced, Installment #{installment.SeqNo} reverted to unpaid.",
                    At = now,
                    By = performedBy,
                });
                plot.UpdatedAt = now;
                plot.UpdatedBy = performedBy;
                await _plotRepository.UpdateAsync(plot, cancellationToken);
            }
        }

        cheque.Status = ChequeStatus.Bounced;
        cheque.BouncePenaltyAmount = (Decimal128)request.PenaltyAmount;
        cheque.UpdatedAt = now;
        cheque.UpdatedBy = performedBy;
        await _chequeRepository.UpdateAsync(cheque, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "ChequeBounced", "Cheque", cheque.Id.ToString(), adminId, new
        {
            PenaltyAmount = request.PenaltyAmount,
            Notes = request.Notes,
            InstallmentSeqNo = installment.SeqNo,
        });

        try
        {
            await _notificationService.SendBouncedChequeAlertAsync(adminId, cheque.Id.ToString(), performedByUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _auditLogger.LogAction(performedByUserId, "BouncedChequeAlertNotificationError", "Cheque", cheque.Id.ToString(), adminId, new { Error = ex.Message });
        }

        return MapToDto(cheque);
    }

    private async Task<Cheque> LoadChequeOrThrowAsync(ObjectId adminObjectId, string chequeId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(chequeId, "chequeId");
        var cheque = await _chequeRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (cheque is null)
        {
            throw new NotFoundAppException($"Cheque '{chequeId}' was not found.");
        }

        return cheque;
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

    private static ChequeResponseDto MapToDto(Cheque cheque) => new(
        cheque.Id.ToString(),
        cheque.AdminId.ToString(),
        cheque.PaymentId.ToString(),
        cheque.ChequeNumber,
        cheque.Bank,
        (decimal)cheque.Amount,
        cheque.DueDate,
        cheque.DepositDate,
        cheque.Status.ToString(),
        cheque.BouncePenaltyAmount is null ? null : (decimal)cheque.BouncePenaltyAmount.Value,
        cheque.CreatedAt,
        cheque.UpdatedAt);
}
