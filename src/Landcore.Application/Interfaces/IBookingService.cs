using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IBookingService
{
    Task<BookingResponseDto> CreateAsync(string adminId, CreateBookingRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<BookingResponseDto> GetByIdAsync(string adminId, string bookingId, CancellationToken cancellationToken = default);

    Task<BookingResponseDto> CancelAsync(string adminId, string bookingId, BookingActionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<BookingResponseDto> ExpireAsync(string adminId, string bookingId, BookingActionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);
}
