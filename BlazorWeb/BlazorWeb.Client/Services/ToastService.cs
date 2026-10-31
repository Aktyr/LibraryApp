namespace BlazorWeb.Client.Services;

public class ToastService : IToastService
{
    private readonly List<ToastMessage> _messages = new();
    public IEnumerable<ToastMessage> Messages => _messages;
    public event Action? OnChange;

    public void ShowSuccess(string message) => ShowToast(message, false);
    public void ShowError(string message) => ShowToast(message, true);

    private void ShowToast(string message, bool isError)
    {
        var toast = new ToastMessage { Message = message, IsError = isError };
        _messages.Add(toast);
        OnChange?.Invoke();

        // Автоматическое скрытие через 4 секунды
        Task.Delay(4000).ContinueWith(_ => Dismiss(toast));
    }

    public void Dismiss(ToastMessage message)
    {
        if (message.IsClosing) return;

        // Включаем анимацию исчезновения
        message.IsClosing = true;
        OnChange?.Invoke();

        // Ждем завершения CSS-анимации и удаляем из списка
        Task.Delay(300).ContinueWith(_ =>
        {
            _messages.Remove(message);
            OnChange?.Invoke();
        });
    }
}