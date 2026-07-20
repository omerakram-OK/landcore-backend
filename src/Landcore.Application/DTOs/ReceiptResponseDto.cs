namespace Landcore.Application.DTOs;

public sealed record ReceiptResponseDto(
    string Id,
    string AdminId,
    string ReceiptNumber,
    string PaymentId,
    DateTime CreatedAt);
