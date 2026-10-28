namespace BlazorWeb.Client.Services.Interfaces;

public interface IConfirmService
{
    event Action? OnChange;

    Task<bool> AskAsync(
        string message,
        string? title = null,
        string confirmText = "OK",
        string cancelText = "Отмена",
        bool requireConfirmationWord = false,
        string confirmationWord = "ПОДТВЕРДИТЬ");

    void Confirm();
    void Cancel();

    bool IsVisible { get; }
    string CurrentTitle { get; }
    string CurrentMessage { get; }
    string CurrentConfirmText { get; }
    string CurrentCancelText { get; }
    bool CurrentRequiresWord { get; }
    string CurrentWord { get; }
    string CurrentButtonClass { get; }
}