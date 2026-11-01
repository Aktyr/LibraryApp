namespace LibApp.Application.Validation.Entities;

public class UserValidatorAsync : IValidator
{
    public Task<ValidationResponse> ValidateEmailAsync(string email, CancellationToken ct = default)
    {
        var errors = EmailPolicy.Validate(email);
        return Task.FromResult(new ValidationResponse(!errors.Any(), errors));
    }

    public Task<ValidationResponse> ValidatePasswordAsync(string password)
    {
        var errors = PasswordPolicy.Validate(password);
        return Task.FromResult(new ValidationResponse(!errors.Any(), errors));
    }

    public Task<ValidationResponse> ValidateNameAndContactAsync(string lastName, string firstName, string middleName, string contactInfo)
    {
        var errors = UserPolicy.ValidateNameAndContact(lastName, firstName, middleName, contactInfo);
        return Task.FromResult(new ValidationResponse(!errors.Any(), errors));
    }

    public async Task<ValidationResponse> ValidateAllAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        var emailResult = await ValidateEmailAsync(email, ct);
        if (!emailResult.IsValid)
            errors.AddRange(emailResult.Errors);

        var passwordResult = await ValidatePasswordAsync(password);
        if (!passwordResult.IsValid)
            errors.AddRange(passwordResult.Errors);

        var nameResult = await ValidateNameAndContactAsync(lastName, firstName, middleName, contactInfo);
        if (!nameResult.IsValid)
            errors.AddRange(nameResult.Errors);

        return new ValidationResponse(!errors.Any(), errors);
    }
}