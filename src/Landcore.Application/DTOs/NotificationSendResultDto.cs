namespace Landcore.Application.DTOs;

public sealed record NotificationSendResultDto(
    bool Sent,
    string NotificationType,
    string RecipientEmail,
    string? FailureReason);
