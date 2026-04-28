namespace LibApp.Core.Configuration;

public class JwtSettings : ISettings
{
    [Required(ErrorMessage = "SecretKey is required")]
    [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "LibraryApp";
    public string Audience { get; set; } = "LibraryApp";

    [Range(1, 1440, ErrorMessage = "ExpiryMinutes must be between 1 and 1440")]
    public int ExpiryMinutes { get; set; } = 60;
}
