namespace Landcore.Application.DTOs;

public sealed record LoginResultDto(string Token, string Role, DateTime ExpiresAtUtc);
