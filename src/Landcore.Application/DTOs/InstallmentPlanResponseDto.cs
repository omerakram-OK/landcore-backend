namespace Landcore.Application.DTOs;

public sealed record InstallmentPlanResponseDto(
    string Id,
    string AdminId,
    string BookingId,
    decimal DownPayment,
    decimal? EarlyPaymentDiscount,
    decimal CreditBalance,
    List<InstallmentDto> Installments,
    DateTime CreatedAt,
    DateTime UpdatedAt);
