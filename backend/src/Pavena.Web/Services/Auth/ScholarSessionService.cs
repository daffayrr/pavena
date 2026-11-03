namespace Pavena.Web.Services.Auth;

public interface IScholarSessionService
{
    Guid CurrentUserId { get; }
    string CurrentUsername { get; }
    string CurrentDisplayName { get; }
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
    event Action? OnSessionChanged;
    void SetSession(Guid userId, string username, string displayName, string token);
    void ClearSession();
}

public class ScholarSessionService : IScholarSessionService
{
    // Default scholar for development and UI inspection
    public Guid CurrentUserId { get; private set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string CurrentUsername { get; private set; } = "marie_curie";
    public string CurrentDisplayName { get; private set; } = "Dr. Marie Curie";
    public string? AccessToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) || CurrentUserId != Guid.Empty;

    public event Action? OnSessionChanged;

    public void SetSession(Guid userId, string username, string displayName, string token)
    {
        CurrentUserId = userId;
        CurrentUsername = username;
        CurrentDisplayName = displayName;
        AccessToken = token;
        OnSessionChanged?.Invoke();
    }

    public void ClearSession()
    {
        CurrentUserId = Guid.Empty;
        CurrentUsername = string.Empty;
        CurrentDisplayName = string.Empty;
        AccessToken = null;
        OnSessionChanged?.Invoke();
    }
}
