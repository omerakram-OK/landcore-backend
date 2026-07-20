namespace Landcore.Common.Helpers;

public static class RegexPatterns
{
    public const string Cnic = @"^\d{5}-\d{7}-\d{1}$";

    public const string Phone = @"^(?:\+92|0)3\d{9}$";
}
