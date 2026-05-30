using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CyberWatchSIEM.Helpers;

public static class SecurityHelper
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static bool VerifyPassword(string password, string hash) =>
        HashPassword(password).Equals(hash, StringComparison.OrdinalIgnoreCase);

    public static bool IsValidUsername(string username) =>
        !string.IsNullOrWhiteSpace(username) && username.Length >= 3 && username.Length <= 50;

    public static bool IsValidPassword(string password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= 6;

    public static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static string Sanitize(string input) =>
        string.IsNullOrEmpty(input) ? string.Empty : Regex.Replace(input.Trim(), @"[<>""']", "");
}
