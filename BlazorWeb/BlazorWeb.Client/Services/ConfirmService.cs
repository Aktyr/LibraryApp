namespace BlazorWeb.Client.Services;

public class ConfirmService : IConfirmService
{
    private TaskCompletionSource<bool>? _tcs;

    public event Action? OnChange;

    public bool IsVisible => _tcs is not null && !_tcs.Task.IsCompleted;
    public string CurrentTitle { get; private set; } = "Подтверждение";
    public string CurrentMessage { get; private set; } = "";
    public string CurrentConfirmText { get; private set; } = "OK";
    public string CurrentCancelText { get; private set; } = "Отмена";
    public bool CurrentRequiresWord { get; private set; }
    public string CurrentWord { get; private set; } = "подтвердить";
    public string CurrentButtonClass => CurrentRequiresWord ? "danger" : "primary";

    public Task<bool> AskAsync(
        string message,
        string? title = null,
        string confirmText = "OK",
        string cancelText = "Отмена",
        bool requireConfirmationWord = false,
        string confirmationWord = "подтвердить")
    {
        // Если предыдущий диалог ещё не закрыт — считаем, что пользователь отменил его.
        _tcs?.TrySetResult(false);

        CurrentTitle = title ?? "Подтверждение";
        CurrentMessage = message;
        CurrentConfirmText = confirmText;
        CurrentCancelText = cancelText;
        CurrentRequiresWord = requireConfirmationWord;
        CurrentWord = string.IsNullOrWhiteSpace(confirmationWord) ? "подтвердить" : confirmationWord.Trim();

        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnChange?.Invoke();
        return _tcs.Task;
    }

    public void Confirm()
    {
        _tcs?.TrySetResult(true);
        OnChange?.Invoke();
    }

    public void Cancel()
    {
        _tcs?.TrySetResult(false);
        OnChange?.Invoke();
    }
}