using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IValidator<CreateBookingRequestDto> _createValidator;
    private readonly IValidator<BookingActionRequestDto> _actionValidator;
    private readonly IAuditLogger _auditLogger;

    private const int DefaultExpiryDays = 15;

    public BookingService(
        IBookingRepository bookingRepository,
        IPlotRepository plotRepository,
        IClientRepository clientRepository,
        ILeadRepository leadRepository,
        IAgentRepository agentRepository,
        IValidator<CreateBookingRequestDto> createValidator,
        IValidator<BookingActionRequestDto> actionValidator,
        IAuditLogger auditLogger)
    {
        _bookingRepository = bookingRepository;
        _plotRepository = plotRepository;
        _clientRepository = clientRepository;
        _leadRepository = leadRepository;
        _agentRepository = agentRepository;
        _createValidator = createValidator;
        _actionValidator = actionValidator;
        _auditLogger = auditLogger;
    }

    public async Task<BookingResponseDto> CreateAsync(string adminId, CreateBookingRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plotId = ParseObjectId(request.PlotId, "PlotId");

        var plot = await _plotRepository.GetByIdAsync(adminObjectId, plotId, cancellationToken);
        if (plot is null)
        {
            throw new ValidationAppException(
                "The Plot was not found for this Admin.",
                new Dictionary<string, string[]> { ["PlotId"] = ["The Plot was not found for this Admin."] });
        }

        if (plot.Status != PlotStatus.Available)
        {
            throw new ValidationAppException(
                $"Plot '{plot.PlotNumber}' is not Available (current status: '{plot.Status}').",
                new Dictionary<string, string[]> { ["PlotId"] = [$"Plot '{plot.PlotNumber}' is not Available (current status: '{plot.Status}')."] });
        }

        var clientId = ParseObjectId(request.ClientId, "ClientId");
        var client = await _clientRepository.GetByIdAsync(adminObjectId, clientId, cancellationToken);
        if (client is null)
        {
            throw new ValidationAppException(
                "The Client was not found for this Admin.",
                new Dictionary<string, string[]> { ["ClientId"] = ["The Client was not found for this Admin."] });
        }

        Lead? lead = null;
        if (!string.IsNullOrWhiteSpace(request.LeadId))
        {
            var leadId = ParseObjectId(request.LeadId, "LeadId");
            lead = await _leadRepository.GetByIdAsync(adminObjectId, leadId, cancellationToken);
            if (lead is null)
            {
                throw new ValidationAppException(
                    "The Lead was not found for this Admin.",
                    new Dictionary<string, string[]> { ["LeadId"] = ["The Lead was not found for this Admin."] });
            }
        }

        Agent? agent = null;
        if (!string.IsNullOrWhiteSpace(request.AgentId))
        {
            var agentId = ParseObjectId(request.AgentId, "AgentId");
            agent = await _agentRepository.GetByIdAsync(adminObjectId, agentId, cancellationToken);
            if (agent is null)
            {
                throw new ValidationAppException(
                    "The Agent was not found for this Admin.",
                    new Dictionary<string, string[]> { ["AgentId"] = ["The Agent was not found for this Admin."] });
            }
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var booking = new Booking
        {
            AdminId = adminObjectId,
            PlotId = plotId,
            ClientId = clientId,
            LeadId = lead?.Id,
            AgentId = agent?.Id,
            CommissionSnapshot = agent is null ? null : new Booking.BookingCommissionSnapshot
            {
                Type = agent.CommissionType,
                Value = agent.CommissionValue,
            },
            TokenAmount = (Decimal128)request.TokenAmount,
            ExpiryDate = request.ExpiryDate ?? now.AddDays(DefaultExpiryDays),
            Status = BookingStatus.Active,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _bookingRepository.CreateAsync(booking, cancellationToken);

        plot.Status = PlotStatus.Booked;
        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "StatusChanged",
            Details = $"Available -> Booked: Booking {booking.Id} created for Client '{client.FullName}'.",
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;
        await _plotRepository.UpdateAsync(plot, cancellationToken);

        if (lead is not null)
        {
            lead.Status = LeadStatus.Converted;
            lead.FollowUpNotes.Add(new Lead.FollowUpNote
            {
                Note = $"Converted — Booking {booking.Id} created for Plot '{plot.PlotNumber}'.",
                By = performedBy,
                At = now,
            });
            lead.UpdatedAt = now;
            lead.UpdatedBy = performedBy;
            await _leadRepository.UpdateAsync(lead, cancellationToken);
        }

        _auditLogger.LogAction(performedByUserId, "BookingCreated", "Booking", booking.Id.ToString(), adminId, new { PlotId = plotId.ToString(), ClientId = clientId.ToString() });

        return MapToDto(booking);
    }

    public async Task<IReadOnlyList<BookingResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var bookings = await _bookingRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<BookingResponseDto> GetByIdAsync(string adminId, string bookingId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var booking = await LoadBookingOrThrowAsync(adminObjectId, bookingId, cancellationToken);
        return MapToDto(booking);
    }

    public async Task<BookingResponseDto> CancelAsync(string adminId, string bookingId, BookingActionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_actionValidator, request, cancellationToken);
        return await TransitionAsync(adminId, bookingId, request, performedByUserId, BookingStatus.Cancelled, "BookingCancelled", cancellationToken);
    }

    public async Task<BookingResponseDto> ExpireAsync(string adminId, string bookingId, BookingActionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_actionValidator, request, cancellationToken);
        return await TransitionAsync(adminId, bookingId, request, performedByUserId, BookingStatus.Expired, "BookingExpired", cancellationToken);
    }

    private async Task<BookingResponseDto> TransitionAsync(string adminId, string bookingId, BookingActionRequestDto request, string performedByUserId, BookingStatus newStatus, string auditAction, CancellationToken cancellationToken)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var booking = await LoadBookingOrThrowAsync(adminObjectId, bookingId, cancellationToken);

        if (booking.Status != BookingStatus.Active)
        {
            throw new ValidationAppException(
                $"Booking cannot be transitioned from '{booking.Status}' (only an Active Booking can be cancelled/expired).",
                new Dictionary<string, string[]> { ["Status"] = [$"Booking is '{booking.Status}', not Active."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        booking.Status = newStatus;
        booking.UpdatedAt = now;
        booking.UpdatedBy = performedBy;
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        var plot = await _plotRepository.GetByIdAsync(adminObjectId, booking.PlotId, cancellationToken);
        if (plot is not null && plot.Status == PlotStatus.Booked)
        {
            plot.Status = PlotStatus.Available;
            plot.HistoryLog.Add(new Plot.HistoryLogEntry
            {
                Event = "StatusChanged",
                Details = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"Booked -> Available: Booking {booking.Id} {newStatus}."
                    : $"Booked -> Available: Booking {booking.Id} {newStatus}: {request.Notes.Trim()}",
                At = now,
                By = performedBy,
            });
            plot.UpdatedAt = now;
            plot.UpdatedBy = performedBy;
            await _plotRepository.UpdateAsync(plot, cancellationToken);
        }

        _auditLogger.LogAction(performedByUserId, auditAction, "Booking", booking.Id.ToString(), adminId);

        return MapToDto(booking);
    }

    private async Task<Booking> LoadBookingOrThrowAsync(ObjectId adminObjectId, string bookingId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(bookingId, "bookingId");
        var booking = await _bookingRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundAppException($"Booking '{bookingId}' was not found.");
        }

        return booking;
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

    private static BookingResponseDto MapToDto(Booking booking) => new(
        booking.Id.ToString(),
        booking.AdminId.ToString(),
        booking.PlotId.ToString(),
        booking.ClientId.ToString(),
        booking.LeadId?.ToString(),
        booking.AgentId?.ToString(),
        booking.CommissionSnapshot?.Type.ToString(),
        booking.CommissionSnapshot is null ? null : (decimal)booking.CommissionSnapshot.Value,
        (decimal)booking.TokenAmount,
        booking.ExpiryDate,
        booking.Status.ToString(),
        booking.CreatedAt,
        booking.UpdatedAt);
}
