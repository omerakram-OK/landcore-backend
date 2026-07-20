using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Configuration;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class RepossessionService : IRepossessionService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IInstallmentPlanRepository _planRepository;
    private readonly IRefundRecordRepository _refundRepository;
    private readonly IValidator<RecordLatePaymentRequestDto> _recordLatePaymentValidator;
    private readonly RepossessionSettings _settings;
    private readonly IAuditLogger _auditLogger;

    public RepossessionService(
        IPlotRepository plotRepository,
        IBookingRepository bookingRepository,
        IInstallmentPlanRepository planRepository,
        IRefundRecordRepository refundRepository,
        IValidator<RecordLatePaymentRequestDto> recordLatePaymentValidator,
        RepossessionSettings settings,
        IAuditLogger auditLogger)
    {
        _plotRepository = plotRepository;
        _bookingRepository = bookingRepository;
        _planRepository = planRepository;
        _refundRepository = refundRepository;
        _recordLatePaymentValidator = recordLatePaymentValidator;
        _settings = settings;
        _auditLogger = auditLogger;
    }

    public async Task<RepossessionScanResultDto> ScanAndFlagOverdueAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var plots = await _plotRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        var newlyOverdue = new List<string>();
        var autoRepossessed = new List<string>();

        foreach (var plot in plots.Where(candidate => candidate.Status is PlotStatus.Sold or PlotStatus.Overdue))
        {
            var booking = await _bookingRepository.GetMostRecentByPlotIdAsync(adminObjectId, plot.Id, cancellationToken);
            if (booking is null || booking.Status != BookingStatus.Converted)
            {
                continue;
            }

            var plan = await _planRepository.GetByBookingIdAsync(adminObjectId, booking.Id, cancellationToken);
            if (plan is null)
            {
                continue;
            }

            var firstMissed = plan.Installments
                .Where(i => i.DueDate < now && (decimal)i.PaidAmount < (decimal)i.Amount)
                .OrderBy(i => i.DueDate)
                .FirstOrDefault();

            if (plot.Status == PlotStatus.Sold)
            {
                if (firstMissed is null)
                {
                    continue;
                }

                foreach (var installment in plan.Installments.Where(i => i.DueDate < now && (decimal)i.PaidAmount < (decimal)i.Amount))
                {
                    installment.Status = InstallmentStatus.Late;
                }

                plan.UpdatedAt = now;
                plan.UpdatedBy = performedBy;
                await _planRepository.UpdateAsync(plan, cancellationToken);

                plot.Status = PlotStatus.Overdue;
                plot.HistoryLog.Add(new Plot.HistoryLogEntry
                {
                    Event = "StatusChanged",
                    Details = $"Sold -> Overdue: Installment #{firstMissed.SeqNo} due {firstMissed.DueDate:yyyy-MM-dd} is unpaid.",
                    At = now,
                    By = performedBy,
                });
                plot.UpdatedAt = now;
                plot.UpdatedBy = performedBy;
                await _plotRepository.UpdateAsync(plot, cancellationToken);

                _auditLogger.LogAction(performedByUserId, "PlotFlaggedOverdue", "Plot", plot.Id.ToString(), adminId,
                    new { InstallmentSeqNo = firstMissed.SeqNo, firstMissed.DueDate });

                newlyOverdue.Add(plot.Id.ToString());
            }
            else
            {
                if (firstMissed is null)
                {
                    continue;
                }

                var repossessionDueDate = firstMissed.DueDate.AddMonths(_settings.GraceMonths);
                if (now < repossessionDueDate)
                {
                    continue;
                }

                foreach (var installment in plan.Installments.Where(i => i.DueDate < now && (decimal)i.PaidAmount < (decimal)i.Amount))
                {
                    installment.Status = InstallmentStatus.Missed;
                }

                plan.UpdatedAt = now;
                plan.UpdatedBy = performedBy;
                await _planRepository.UpdateAsync(plan, cancellationToken);

                plot.Status = PlotStatus.Repossessed;
                plot.HistoryLog.Add(new Plot.HistoryLogEntry
                {
                    Event = "StatusChanged",
                    Details = $"Overdue -> Repossessed: grace period ({_settings.GraceMonths} month(s)) elapsed since Installment #{firstMissed.SeqNo} (due {firstMissed.DueDate:yyyy-MM-dd}).",
                    At = now,
                    By = performedBy,
                });
                plot.UpdatedAt = now;
                plot.UpdatedBy = performedBy;
                await _plotRepository.UpdateAsync(plot, cancellationToken);

                _auditLogger.LogAction(performedByUserId, "PlotRepossessed", "Plot", plot.Id.ToString(), adminId,
                    new { InstallmentSeqNo = firstMissed.SeqNo, firstMissed.DueDate });

                plot.Status = PlotStatus.Available;
                plot.HistoryLog.Add(new Plot.HistoryLogEntry
                {
                    Event = "StatusChanged",
                    Details = "Repossessed -> Available: returned to inventory for resale.",
                    At = now,
                    By = performedBy,
                });
                plot.UpdatedAt = now;
                plot.UpdatedBy = performedBy;
                await _plotRepository.UpdateAsync(plot, cancellationToken);

                _auditLogger.LogAction(performedByUserId, "PlotReturnedToInventory", "Plot", plot.Id.ToString(), adminId);

                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = now;
                booking.UpdatedBy = performedBy;
                await _bookingRepository.UpdateAsync(booking, cancellationToken);

                _auditLogger.LogAction(performedByUserId, "BookingCancelledByRepossession", "Booking", booking.Id.ToString(), adminId);

                autoRepossessed.Add(plot.Id.ToString());
            }
        }

        return new RepossessionScanResultDto(newlyOverdue, autoRepossessed);
    }

    public async Task<PlotResponseDto> ResumePlanAsync(string adminId, string plotId, string? notes, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        if (plot.Status != PlotStatus.Overdue)
        {
            throw new ValidationAppException(
                $"Plot cannot resume its plan while its status is '{plot.Status}' (must be Overdue).",
                new Dictionary<string, string[]> { ["Status"] = [$"Plot is '{plot.Status}', not Overdue."] });
        }

        var booking = await _bookingRepository.GetMostRecentByPlotIdAsync(adminObjectId, plot.Id, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Converted)
        {
            throw new ValidationAppException(
                "No active (Converted) Booking was found for this Plot to resume.",
                new Dictionary<string, string[]> { ["PlotId"] = ["No active (Converted) Booking was found for this Plot to resume."] });
        }

        var plan = await _planRepository.GetByBookingIdAsync(adminObjectId, booking.Id, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (plan is not null)
        {
            foreach (var installment in plan.Installments.Where(i => i.Status == InstallmentStatus.Late))
            {
                installment.Status = InstallmentStatus.Pending;
            }

            plan.UpdatedAt = now;
            plan.UpdatedBy = performedBy;
            await _planRepository.UpdateAsync(plan, cancellationToken);
        }

        plot.Status = PlotStatus.Sold;
        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "StatusChanged",
            Details = trimmedNotes is null
                ? "Overdue -> Sold: plan resumed via approved RepossessionOverride."
                : $"Overdue -> Sold: plan resumed via approved RepossessionOverride: {trimmedNotes}",
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "PlotRepossessionOverrideResumed", "Plot", plot.Id.ToString(), adminId);

        return MapPlotToDto(plot);
    }

    public async Task<RefundRecordResponseDto> RecordLatePaymentAsync(string adminId, string plotId, RecordLatePaymentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_recordLatePaymentValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var booking = await _bookingRepository.GetMostRecentByPlotIdAsync(adminObjectId, plot.Id, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Cancelled)
        {
            throw new ValidationAppException(
                "This Plot has no former (auto-repossessed) Booking to record a late payment against.",
                new Dictionary<string, string[]> { ["PlotId"] = ["This Plot has no former (auto-repossessed) Booking to record a late payment against."] });
        }

        var plan = await _planRepository.GetByBookingIdAsync(adminObjectId, booking.Id, cancellationToken);
        if (plan is null)
        {
            throw new ValidationAppException(
                "No InstallmentPlan was found for the former Booking on this Plot.",
                new Dictionary<string, string[]> { ["PlotId"] = ["No InstallmentPlan was found for the former Booking on this Plot."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var companyProfit = Math.Round(request.AmountPaid * 0.15m, 2, MidpointRounding.AwayFromZero);
        var clientRefund = request.AmountPaid - companyProfit;

        var record = new RefundRecord
        {
            AdminId = adminObjectId,
            PlotId = plot.Id,
            BookingId = booking.Id,
            InstallmentPlanId = plan.Id,
            ClientId = booking.ClientId,
            AmountPaid = (Decimal128)request.AmountPaid,
            CompanyProfitAmount = (Decimal128)companyProfit,
            ClientRefundAmount = (Decimal128)clientRefund,
            PaymentDate = request.PaymentDate,
            Status = RefundRecordStatus.PendingIssuance,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _refundRepository.CreateAsync(record, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LatePaymentRecorded", "RefundRecord", record.Id.ToString(), adminId,
            new { PlotId = plot.Id.ToString(), request.AmountPaid, CompanyProfitAmount = companyProfit, ClientRefundAmount = clientRefund });

        return MapRefundToDto(record);
    }

    public async Task<RefundRecordResponseDto> IssueRefundAsync(string adminId, string refundRecordId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var record = await LoadRefundOrThrowAsync(adminObjectId, refundRecordId, cancellationToken);

        if (record.Status != RefundRecordStatus.PendingIssuance)
        {
            throw new ValidationAppException(
                $"RefundRecord is already '{record.Status}'.",
                new Dictionary<string, string[]> { ["Status"] = [$"RefundRecord is already '{record.Status}'."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        record.Status = RefundRecordStatus.Issued;
        record.IssuedAt = now;
        record.IssuedBy = performedBy;
        record.UpdatedAt = now;
        record.UpdatedBy = performedBy;

        await _refundRepository.UpdateAsync(record, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "RefundIssued", "RefundRecord", record.Id.ToString(), adminId);

        return MapRefundToDto(record);
    }

    public async Task<IReadOnlyList<RefundRecordResponseDto>> GetAllRefundsAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var records = await _refundRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return records.Select(MapRefundToDto).ToList();
    }

    public async Task<IReadOnlyList<RefundRecordResponseDto>> GetRefundsByPlotIdAsync(string adminId, string plotId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(plotId, "plotId");
        var records = await _refundRepository.GetByPlotIdAsync(adminObjectId, id, cancellationToken);
        return records.Select(MapRefundToDto).ToList();
    }

    public async Task<RefundRecordResponseDto> GetRefundByIdAsync(string adminId, string refundRecordId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var record = await LoadRefundOrThrowAsync(adminObjectId, refundRecordId, cancellationToken);
        return MapRefundToDto(record);
    }

    private async Task<Plot> LoadPlotOrThrowAsync(ObjectId adminObjectId, string plotId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(plotId, "plotId");
        var plot = await _plotRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (plot is null)
        {
            throw new NotFoundAppException($"Plot '{plotId}' was not found.");
        }

        return plot;
    }

    private async Task<RefundRecord> LoadRefundOrThrowAsync(ObjectId adminObjectId, string refundRecordId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(refundRecordId, "refundRecordId");
        var record = await _refundRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (record is null)
        {
            throw new NotFoundAppException($"RefundRecord '{refundRecordId}' was not found.");
        }

        return record;
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

    private static PlotResponseDto MapPlotToDto(Plot plot) => new(
        plot.Id.ToString(),
        plot.AdminId.ToString(),
        plot.PlotNumber,
        plot.BlockId.ToString(),
        plot.SocietyId.ToString(),
        plot.Size,
        plot.SizeUnit.ToString(),
        plot.Category.ToString(),
        (decimal)plot.BasePrice,
        plot.Charges.Select(charge => new PlotChargeDto(charge.ChargeType, (decimal)charge.Amount)).ToList(),
        (decimal)plot.AnnualMaintenanceCharge,
        plot.Status.ToString(),
        plot.PossessionStatus.ToString(),
        plot.OwnerClientIds.Select(id => id.ToString()).ToList(),
        plot.HistoryLog.Select(entry => new PlotHistoryLogEntryDto(entry.Event, entry.Details, entry.At, entry.By.ToString())).ToList(),
        plot.IsDeleted,
        plot.CreatedAt,
        plot.UpdatedAt);

    private static RefundRecordResponseDto MapRefundToDto(RefundRecord record) => new(
        record.Id.ToString(),
        record.AdminId.ToString(),
        record.PlotId.ToString(),
        record.BookingId.ToString(),
        record.InstallmentPlanId.ToString(),
        record.ClientId.ToString(),
        (decimal)record.AmountPaid,
        (decimal)record.CompanyProfitAmount,
        (decimal)record.ClientRefundAmount,
        record.PaymentDate,
        record.Status.ToString(),
        record.IssuedAt,
        record.IssuedBy?.ToString(),
        record.Notes,
        record.CreatedAt,
        record.UpdatedAt);
}
