namespace LibApp.Application.Configuration;
public class DatabaseSettings : ISettings
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
