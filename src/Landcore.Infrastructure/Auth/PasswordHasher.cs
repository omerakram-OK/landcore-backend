using Landcore.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Landcore.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    private static readonly object UserPlaceholder = new();

    public string Hash(string password) => _hasher.HashPassword(UserPlaceholder, password);

    public bool Verify(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(UserPlaceholder, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
