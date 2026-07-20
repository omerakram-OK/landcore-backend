namespace Landcore.Application.Interfaces;

public sealed record JwtIssueRequest(
    string UserId,
    string Role,
    string? AdminId,
    IReadOnlyCollection<string> Permissions,
    string? Email = null,
    string? FullName = null);

public sealed record JwtIssueResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    JwtIssueResult GenerateToken(JwtIssueRequest request);
}
