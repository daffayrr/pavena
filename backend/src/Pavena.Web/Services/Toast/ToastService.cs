namespace Pavena.Web.Services.Toast;

public enum ToastType
{
    Info,
    Success,
    Warning,
    Danger
}

public record ToastMessage(
    Guid Id,
    string Title,
    string Message,
    ToastType Type,
    int DurationMs = 4000
);

public interface IToastService
{
    IReadOnlyList<ToastMessage> Toasts { get; }
    event Action? OnToastsChanged;
    void Show(string title, string message, ToastType type = ToastType.Info, int durationMs = 4000);
    void Success(string message, string title = "Berhasil");
    void Error(string message, string title = "Kesalahan");
    void Info(string message, string title = "Informasi");
    void Warning(string message, string title = "Perhatian");
    void Dismiss(Guid id);
}

public class ToastService : IToastService
{
    private readonly List<ToastMessage> _toasts = new();
    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public event Action? OnToastsChanged;

    public void Show(string title, string message, ToastType type = ToastType.Info, int durationMs = 4000)
    {
        var toast = new ToastMessage(Guid.NewGuid(), title, message, type, durationMs);
        _toasts.Add(toast);
        OnToastsChanged?.Invoke();

        if (durationMs > 0)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(durationMs);
                Dismiss(toast.Id);
            });
        }
    }

    public void Success(string message, string title = "Berhasil") => Show(title, message, ToastType.Success);
    public void Error(string message, string title = "Kesalahan") => Show(title, message, ToastType.Danger, 6000);
    public void Info(string message, string title = "Informasi") => Show(title, message, ToastType.Info);
    public void Warning(string message, string title = "Perhatian") => Show(title, message, ToastType.Warning, 5000);

    public void Dismiss(Guid id)
    {
        var item = _toasts.FirstOrDefault(t => t.Id == id);
        if (item != null)
        {
            _toasts.Remove(item);
            OnToastsChanged?.Invoke();
        }
    }
}
