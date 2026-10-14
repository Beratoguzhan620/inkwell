namespace BlogApp.Data.Constants;

public static class RoleConstants
{
    public const int AdminId = 1;
    public const int AuthorId = 2;
    public const int ReaderId = 3;

    /// <summary>Manages content and users from the admin panel; can also write.</summary>
    public const string Admin = "Admin";

    /// <summary>Writes and publishes posts. New accounts get this role once verified.</summary>
    public const string Author = "Author";

    /// <summary>Reads and comments only.</summary>
    public const string Reader = "Reader";

    public const string CanWrite = Admin + "," + Author;

    public static readonly IReadOnlyDictionary<int, string> All = new Dictionary<int, string>
    {
        [AdminId] = Admin,
        [AuthorId] = Author,
        [ReaderId] = Reader,
    };
}
