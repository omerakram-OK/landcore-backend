namespace Landcore.Application.DTOs;

public sealed record CreateInstallmentPlanRequestDto(
    string BookingId,
    decimal DownPayment,
    decimal? EarlyPaymentDiscount,
    List<CreateInstallmentDto> Installments);
