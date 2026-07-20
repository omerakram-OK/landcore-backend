using Landcore.Domain.Common;

namespace Landcore.Domain.Entities;

public class BankAccount : TenantEntity
{
    public string AccountName { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;
}
