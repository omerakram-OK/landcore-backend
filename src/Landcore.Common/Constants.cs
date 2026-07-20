namespace Landcore.Common;

public static class Constants
{
    public static class Roles
    {
        public const string SuperMan = "SuperMan";
        public const string Admin = "Admin";
        public const string Employee = "Employee";
    }

    public static class ClaimTypes
    {
        public const string UserId = System.Security.Claims.ClaimTypes.NameIdentifier;
        public const string Role = System.Security.Claims.ClaimTypes.Role;

        public const string AdminId = "adminId";

        public const string Permission = "permission";
    }
}
