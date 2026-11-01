namespace LibApp.Core.Policies;

public static class EmailPolicy
{
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
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

    public static List<string> Validate(string? email)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email обязателен");
        else if (!IsValid(email))
            errors.Add("Некорректный формат email");

        return errors;
    }
}