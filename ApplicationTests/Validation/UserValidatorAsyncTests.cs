namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class UserValidatorAsyncTests
{
    private static UserValidatorAsync CreateValidator() => new(new EmailValidatorAsync());

    #region ValidateEmailAsync

    [Test]
    public async Task ValidateEmailAsync_WithValidEmail_ReturnsValid()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateEmailAsync("test@test.com");
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task ValidateEmailAsync_WithInvalidEmail_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateEmailAsync("invalid-email");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Некорректный формат email"));
        });
    }

    [Test]
    public async Task ValidateEmailAsync_WithEmptyEmail_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateEmailAsync("");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Email обязателен"));
        });
    }

    #endregion

    #region ValidatePasswordAsync

    [Test]
    public async Task ValidatePasswordAsync_WithValidPassword_ReturnsValid()
    {
        var validator = CreateValidator();
        var result = await validator.ValidatePasswordAsync("Password123!");
        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("password123!", "Пароль должен содержать хотя бы одну заглавную букву")]
    [TestCase("PASSWORD123!", "Пароль должен содержать хотя бы одну строчную букву")]
    [TestCase("Password!", "Пароль должен содержать хотя бы одну цифру")]
    [TestCase("Password123", "Пароль должен содержать хотя бы один спецсимвол")]
    [TestCase("Pass1!", "Пароль должен быть не менее 8 символов")]
    [TestCase("", "Пароль обязателен")]
    public async Task ValidatePasswordAsync_WeakPassword_ReturnsExpectedError(string password, string expectedError)
    {
        var validator = CreateValidator();
        var result = await validator.ValidatePasswordAsync(password);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member(expectedError));
        });
    }

    [Test]
    public async Task ValidatePasswordAsync_WithTooLongPassword_ReturnsError()
    {
        var validator = CreateValidator();
        var password = new string('A', 101);
        var result = await validator.ValidatePasswordAsync(password);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Пароль слишком длинный"));
        });
    }

    #endregion

    #region ValidateNameAndContactAsync

    [Test]
    public async Task ValidateNameAndContactAsync_WithValidData_ReturnsValid()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync("Иванов", "Иван", "Иванович", "info@test.com");
        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("", "First", "Middle", "info", "Фамилия обязательна")]
    [TestCase("Last", "", "Middle", "info", "Имя обязательно")]
    [TestCase("Last", "First", "", "", "Контактная информация обязательна")]
    public async Task ValidateNameAndContactAsync_WithEmptyRequiredFields_ReturnsError(
        string lastName, string firstName, string middleName, string contactInfo, string expectedError)
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync(lastName, firstName, middleName, contactInfo);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member(expectedError));
        });
    }

    [Test]
    public async Task ValidateNameAndContactAsync_WhenLastNameTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync(new string('A', 101), "First", "Middle", "info");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Фамилия не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateNameAndContactAsync_WhenFirstNameTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync("Last", new string('B', 101), "Middle", "info");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Имя не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateNameAndContactAsync_WhenMiddleNameTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync("Last", "First", new string('C', 101), "info");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Отчество не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateNameAndContactAsync_WhenContactInfoTooLong_ReturnsError()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync("Last", "First", "Middle", new string('D', 201));
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Member("Контактная информация не может превышать 200 символов"));
        });
    }

    [Test]
    public async Task ValidateNameAndContactAsync_WhenNamesExactlyMaxLength_ReturnsValid()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateNameAndContactAsync(
            new string('A', 100),
            new string('B', 100),
            new string('C', 100),
            "info@test.com"
        );
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region ValidateAllAsync

    [Test]
    public async Task ValidateAllAsync_WithValidData_ReturnsValid()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("test@test.com", "Password123!", "Иванов", "Иван", "Иванович", "info");
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task ValidateAllAsync_WithMultipleErrors_ReturnsAllErrors()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAllAsync("invalid", "123", "", "First", "Middle", "");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.GreaterThanOrEqualTo(3));
        });
    }

    #endregion
}