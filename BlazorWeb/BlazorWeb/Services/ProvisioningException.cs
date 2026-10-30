namespace BlazorWeb.Services;

public enum ProvisioningErrorKind
{
    IdentityError,
    LibraryError,
    Unknown
}

public class ProvisioningException : Exception
{
    public ProvisioningErrorKind Kind { get; }
    public IReadOnlyList<string> Errors { get; }

    public ProvisioningException(ProvisioningErrorKind kind, IReadOnlyList<string> errors, Exception? inner = null)
        : base(string.Join("; ", errors), inner)
    {
        Kind = kind;
        Errors = errors;
    }
}