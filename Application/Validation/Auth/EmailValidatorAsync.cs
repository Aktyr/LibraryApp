namespace LibApp.Application.Validation.Auth;

public class EmailValidatorAsync : IValidator
{
    public async Task<ValidationResponse> ValidateAsync(string email, CancellationToken cancellationToken = default)
    {
        var errors = EmailPolicy.Validate(email);

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }
}