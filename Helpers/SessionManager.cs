using CyberWatchSIEM.Models;

namespace CyberWatchSIEM.Helpers;

public static class SessionManager
{
    public static User? CurrentUser { get; private set; }
    public static bool RememberMe { get; set; }
    public static string? RememberedUsername { get; set; }

    public static void StartSession(User user, bool remember = false)
    {
        CurrentUser = user;
        RememberMe = remember;
        if (remember)
            RememberedUsername = user.Username;
        else
            RememberedUsername = null;
    }

    public static void EndSession()
    {
        CurrentUser = null;
        if (!RememberMe)
            RememberedUsername = null;
    }

    public static bool IsAuthenticated => CurrentUser != null;

    public static bool IsAdmin =>
        CurrentUser?.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
}
