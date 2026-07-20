namespace Landcore.Application.DTOs;

public sealed record NotificationScanResultDto(
    int NotificationsSent,
    int NotificationsFailed,
    List<string> Details);
