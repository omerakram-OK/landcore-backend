using Landcore.Domain.Common;

namespace Landcore.Domain.Entities;

public class SuperMan : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
