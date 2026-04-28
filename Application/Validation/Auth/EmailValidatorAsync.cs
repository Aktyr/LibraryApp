namespace LibApp.Application.Validation.Auth;

public class EmailValidatorAsync : IValidator
{
    public async Task<ValidationResponse> ValidateAsync(string email, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email обязателен");
        else if (!IsValidEmail(email))
            errors.Add("Некорректный формат email");

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

}
