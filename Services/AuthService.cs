using CyberWatchSIEM.Database;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Services;

public class AuthService
{
    private readonly UserRepository _users;
    private readonly AuditRepository _audit;

    public AuthService(ApplicationDbContext context)
    {
        _users = new UserRepository(context);
        _audit = new AuditRepository(context);
    }

    public async Task<(bool Success, User? User, string Message)> LoginAsync(string username, string password)
    {
        if (!SecurityHelper.IsValidUsername(username) || !SecurityHelper.IsValidPassword(password))
            return (false, null, "Invalid credentials format.");

        var user = await _users.GetByUsernameAsync(SecurityHelper.Sanitize(username));
        if (user == null || !SecurityHelper.VerifyPassword(password, user.PasswordHash))
            return (false, null, "Invalid username or password.");

        user.LastLogin = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await _audit.LogAsync(user.Username, "Login", "User", "User logged in successfully");

        return (true, user, "Login successful.");
    }

    public async Task LogoutAsync()
    {
        if (SessionManager.CurrentUser != null)
            await _audit.LogAsync(SessionManager.CurrentUser.Username, "Logout", "User", "User logged out");
        SessionManager.EndSession();
    }
}

public class AuditService
{
    private readonly AuditRepository _audit;

    public AuditService(ApplicationDbContext context) => _audit = new AuditRepository(context);

    public Task LogCreateAsync(string entityType, string details) =>
        LogAsync("Create", entityType, details);

    public Task LogUpdateAsync(string entityType, string details) =>
        LogAsync("Update", entityType, details);

    public Task LogDeleteAsync(string entityType, string details) =>
        LogAsync("Delete", entityType, details);

    private Task LogAsync(string action, string entityType, string details)
    {
        var username = SessionManager.CurrentUser?.Username ?? "System";
        return _audit.LogAsync(username, action, entityType, details);
    }

    public Task<List<AuditLog>> GetAllAsync() => _audit.GetAllAsync();
}
