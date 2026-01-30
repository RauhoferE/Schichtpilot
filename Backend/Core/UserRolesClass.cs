namespace Core;

public static class UserRolesClass
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static List<string> AllRoles = new List<string>()
    {
        Admin,
        User
    };
}