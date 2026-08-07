namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class UserRegistrationValidatorTests
{
    private UserRegistrationValidator CreateValidator() => new(new EmailValidatorAsync());

    [Test]
    public async Task ValidateAsync_WithValidData_ReturnsValid()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Иванов", "Иван", "Иванович", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [TestCase("password123!", "Пароль должен содержать хотя бы одну заглавную букву")]
    [TestCase("PASSWORD123!", "Пароль должен содержать хотя бы одну строчную букву")]
    [TestCase("Password!", "Пароль должен содержать хотя бы одну цифру")]
    [TestCase("Password123", "Пароль должен содержать хотя бы один спецсимвол")]
    [TestCase("Pass1!", "Пароль должен быть не менее 8 символов")]
    public async Task ValidateAsync_WeakPassword_ReturnsExpectedError(string password, string expectedError)
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", password, "Last", "First", "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member(expectedError));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenEmailInvalid_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("invalid-email", "Password123!", "Last", "First", "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Некорректный формат email"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenEmailEmpty_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("", "Password123!", "Last", "First", "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Email обязателен"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenLastNameEmpty_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "", "First", "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Фамилия обязательна"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenFirstNameEmpty_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Last", "", "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Имя обязательно"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenContactInfoEmpty_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Last", "First", "", "");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Контактная информация обязательна"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenLastNameTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", new string('A', 101), "First", "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Фамилия не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenFirstNameTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Last", new string('B', 101), "", "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Имя не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenMiddleNameTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Last", "First", new string('C', 101), "info");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Отчество не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenContactInfoTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Last", "First", "", new string('D', 201));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Контактная информация не может превышать 200 символов"));
        });
    }
}