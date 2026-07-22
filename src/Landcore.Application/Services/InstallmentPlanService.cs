using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class InstallmentPlanService : IInstallmentPlanService
{
    private readonly IInstallmentPlanRepository _planRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IValidator<CreateInstallmentPlanRequestDto> _createValidator;
    private readonly IValidator<ApplyDiscountRequestDto> _applyDiscountValidator;
    private readonly IValidator<UpdateInstallmentPlanRequestDto> _updateScheduleValidator;
    private readonly IAuditLogger _auditLogger;

    public InstallmentPlanService(
        IInstallmentPlanRepository planRepository,
        IBookingRepository bookingRepository,
        IValidator<CreateInstallmentPlanRequestDto> createValidator,
        IValidator<ApplyDiscountRequestDto> applyDiscountValidator,
        IValidator<UpdateInstallmentPlanRequestDto> updateScheduleValidator,
        IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _bookingRepository = bookingRepository;
        _createValidator = createValidator;
        _applyDiscountValidator = applyDiscountValidator;
        _updateScheduleValidator = updateScheduleValidator;
        _auditLogger = auditLogger;
    }

    public async Task<InstallmentPlanResponseDto> CreateAsync(string adminId, CreateInstallmentPlanRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var bookingId = ParseObjectId(request.BookingId, "BookingId");

        var booking = await _bookingRepository.GetByIdAsync(adminObjectId, bookingId, cancellationToken);
        if (booking is null)
        {
            throw new ValidationAppException(
                "The Booking was not found for this Admin.",
                new Dictionary<string, string[]> { ["BookingId"] = ["The Booking was not found for this Admin."] });
        }

        if (booking.Status != BookingStatus.Active)
        {
            throw new ValidationAppException(
                $"Booking cannot have an InstallmentPlan set up while its status is '{booking.Status}' (must be Active).",
                new Dictionary<string, string[]> { ["BookingId"] = [$"Booking is '{booking.Status}', not Active."] });
        }

        var existingPlan = await _planRepository.GetByBookingIdAsync(adminObjectId, bookingId, cancellationToken);
        if (existingPlan is not null)
        {
            throw new ValidationAppException(
                "An InstallmentPlan already exists for this Booking.",
                new Dictionary<string, string[]> { ["BookingId"] = ["An InstallmentPlan already exists for this Booking."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var installments = request.Installments.Select((dto, index) => new Installment
        {
            SeqNo = index + 1,
            DueDate = dto.DueDate,
            Amount = (Decimal128)dto.Amount,
            Status = InstallmentStatus.Pending,
            PaidAmount = Decimal128.Zero,
        }).ToList();

        var plan = new InstallmentPlan
        {
            AdminId = adminObjectId,
            BookingId = bookingId,
            DownPayment = (Decimal128)request.DownPayment,
            EarlyPaymentDiscount = request.EarlyPaymentDiscount is null ? null : (Decimal128)request.EarlyPaymentDiscount.Value,
            Installments = installments,
            CreditBalance = Decimal128.Zero,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _planRepository.CreateAsync(plan, cancellationToken);

        booking.Status = BookingStatus.Converted;
        booking.UpdatedAt = now;
        booking.UpdatedBy = performedBy;
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "InstallmentPlanCreated", "InstallmentPlan", plan.Id.ToString(), adminId, new { BookingId = bookingId.ToString(), InstallmentCount = installments.Count });

        return MapToDto(plan);
    }

    public async Task<IReadOnlyList<InstallmentPlanResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plans = await _planRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return plans.Select(MapToDto).ToList();
    }

    public async Task<InstallmentPlanResponseDto> GetByIdAsync(string adminId, string planId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plan = await LoadPlanOrThrowAsync(adminObjectId, planId, cancellationToken);
        return MapToDto(plan);
    }

    public async Task<InstallmentPlanResponseDto> GetByBookingIdAsync(string adminId, string bookingId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(bookingId, "bookingId");
        var plan = await _planRepository.GetByBookingIdAsync(adminObjectId, id, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundAppException($"No InstallmentPlan was found for Booking '{bookingId}'.");
        }

        return MapToDto(plan);
    }

    public async Task<InstallmentPlanResponseDto> ApplyDiscountAsync(string adminId, string planId, ApplyDiscountRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_applyDiscountValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plan = await LoadPlanOrThrowAsync(adminObjectId, planId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        plan.CreditBalance = (Decimal128)((decimal)plan.CreditBalance + request.DiscountAmount);
        plan.UpdatedAt = now;
        plan.UpdatedBy = performedBy;

        await _planRepository.UpdateAsync(plan, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "InstallmentPlanDiscountApplied", "InstallmentPlan", plan.Id.ToString(), adminId,
            new { DiscountAmount = request.DiscountAmount, request.Notes, request.Justification });

        return MapToDto(plan);
    }

    public async Task<InstallmentPlanResponseDto> UpdateScheduleAsync(string adminId, string planId, UpdateInstallmentPlanRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateScheduleValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plan = await LoadPlanOrThrowAsync(adminObjectId, planId, cancellationToken);

        var existingBySeqNo = plan.Installments.ToDictionary(installment => installment.SeqNo);

        foreach (var existing in plan.Installments)
        {
            var hasRecordedPayment = (decimal)existing.PaidAmount > 0;
            if (!hasRecordedPayment)
            {
                continue;
            }

            var stillIncluded = request.Installments.Any(item => item.SeqNo == existing.SeqNo);
            if (!stillIncluded)
            {
                throw new ValidationAppException(
                    $"Installment #{existing.SeqNo} already has a recorded payment and cannot be removed.",
                    new Dictionary<string, string[]> { ["Installments"] = [$"Installment #{existing.SeqNo} already has a recorded payment and cannot be removed."] });
            }
        }

        var nextNewSeqNo = plan.Installments.Count == 0 ? 1 : plan.Installments.Max(installment => installment.SeqNo) + 1;
        var updatedInstallments = new List<Installment>();

        foreach (var item in request.Installments)
        {
            if (item.SeqNo.HasValue && existingBySeqNo.TryGetValue(item.SeqNo.Value, out var existing))
            {
                var paidAmount = (decimal)existing.PaidAmount;

                if (item.Amount < paidAmount)
                {
                    throw new ValidationAppException(
                        $"Installment #{existing.SeqNo} already has {paidAmount} paid against it and cannot be reduced below that amount.",
                        new Dictionary<string, string[]> { ["Installments"] = [$"Installment #{existing.SeqNo} already has {paidAmount} paid against it and cannot be reduced below that amount."] });
                }

                if (existing.Status == InstallmentStatus.Paid && (item.Amount != paidAmount || item.DueDate != existing.DueDate))
                {
                    throw new ValidationAppException(
                        $"Installment #{existing.SeqNo} is already fully paid and cannot be changed.",
                        new Dictionary<string, string[]> { ["Installments"] = [$"Installment #{existing.SeqNo} is already fully paid and cannot be changed."] });
                }

                updatedInstallments.Add(new Installment
                {
                    SeqNo = existing.SeqNo,
                    DueDate = item.DueDate,
                    Amount = (Decimal128)item.Amount,
                    Status = existing.Status,
                    PaidAmount = existing.PaidAmount,
                });
            }
            else
            {
                updatedInstallments.Add(new Installment
                {
                    SeqNo = nextNewSeqNo++,
                    DueDate = item.DueDate,
                    Amount = (Decimal128)item.Amount,
                    Status = InstallmentStatus.Pending,
                    PaidAmount = Decimal128.Zero,
                });
            }
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        plan.Installments = updatedInstallments.OrderBy(installment => installment.DueDate).ToList();
        plan.UpdatedAt = DateTime.UtcNow;
        plan.UpdatedBy = performedBy;

        await _planRepository.UpdateAsync(plan, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "InstallmentPlanScheduleUpdated", "InstallmentPlan", plan.Id.ToString(), adminId, new { InstallmentCount = updatedInstallments.Count });

        return MapToDto(plan);
    }

    private async Task<InstallmentPlan> LoadPlanOrThrowAsync(ObjectId adminObjectId, string planId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(planId, "planId");
        var plan = await _planRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundAppException($"InstallmentPlan '{planId}' was not found.");
        }

        return plan;
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

    private static InstallmentPlanResponseDto MapToDto(InstallmentPlan plan) => new(
        plan.Id.ToString(),
        plan.AdminId.ToString(),
        plan.BookingId.ToString(),
        (decimal)plan.DownPayment,
        plan.EarlyPaymentDiscount is null ? null : (decimal)plan.EarlyPaymentDiscount.Value,
        (decimal)plan.CreditBalance,
        plan.Installments.Select(i => new InstallmentDto(i.SeqNo, i.DueDate, (decimal)i.Amount, i.Status.ToString(), (decimal)i.PaidAmount)).ToList(),
        plan.CreatedAt,
        plan.UpdatedAt);
}
