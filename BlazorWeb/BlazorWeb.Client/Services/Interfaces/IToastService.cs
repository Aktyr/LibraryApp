namespace BlazorWeb.Client.Services.Interfaces;

public interface IToastService
{
    event Action? OnChange;
    IEnumerable<ToastMessage> Messages { get; }

    void ShowSuccess(string message);
    void ShowError(string message);
    void Dismiss(ToastMessage message);
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; set; } = "";
    public bool IsError { get; set; }
    public bool IsClosing { get; set; }

}