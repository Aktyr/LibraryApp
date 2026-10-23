namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class JwtServiceTests
{
    [Test]
    public void GenerateToken_WithIdentityData_ReturnsValidToken()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-for-testing-purposes-only-12345",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = 60
        });
        var service = new JwtService(settings);
        var libraryUserId = Guid.NewGuid();

        var token = service.GenerateToken(libraryUserId, "test@test.com", "Иванов Иван", UserRole.Reader);

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.Not.Empty);
            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(token);
            Assert.That(parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value,
                Is.EqualTo(libraryUserId.ToString()));
            Assert.That(parsed.Claims.First(c => c.Type == ClaimTypes.Role).Value, Is.EqualTo("Reader"));
        });
    }
}