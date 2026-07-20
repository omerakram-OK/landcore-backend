namespace Landcore.Application.DTOs;

public sealed record RecordLatePaymentRequestDto(
    decimal AmountPaid,
    DateTime PaymentDate,
    string? Notes);
