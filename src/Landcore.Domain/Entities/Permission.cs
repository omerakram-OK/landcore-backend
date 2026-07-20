namespace Landcore.Domain.Entities;

public class Permission
{
    public string Module { get; set; } = string.Empty;

    public List<string> Actions { get; set; } = new();
}
