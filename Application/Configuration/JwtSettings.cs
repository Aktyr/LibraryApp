namespace LibApp.Application.Configuration;

public class JwtSettings : ISettings
{
    [Required(ErrorMessage = "SecretKey is required")]
    [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "LibraryApp";
    public string Audience { get; set; } = "LibraryApp";

    [PositiveNumber] [MaxRange(1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
