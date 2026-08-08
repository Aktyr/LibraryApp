namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class UserServiceTests
{
    private static UserService CreateService(FakeUnitOfWork unitOfWork, UserValidatorAsync? validator = null)
    {
        validator ??= new UserValidatorAsync(new EmailValidatorAsync());
        return new UserService(unitOfWork, validator);
    }

    [Test]
    public async Task CreateUserAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(unitOfWork);
        var email = "test@test.com";
        var password = "Password123!";
        var lastName = "Иванов";
        var firstName = "Иван";
        var middleName = "Иванович";
        var contactInfo = "+7-999-123-45-67";
        var role = UserRole.Reader;

        // Act
        var user = await service.CreateUserAsync(email, password, lastName, firstName, middleName, contactInfo, role);

        // Assert
        var repo = unitOfWork.GetRepository<User>();
        var saved = await repo.FirstOrDefaultAsync(u => u.Email == email);
        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.LastName, Is.EqualTo(lastName));
            Assert.That(saved.FirstName, Is.EqualTo(firstName));
            Assert.That(saved.MiddleName, Is.EqualTo(middleName));
            Assert.That(saved.ContactInfo, Is.EqualTo(contactInfo));
            Assert.That(saved.Role, Is.EqualTo(role));
            Assert.That(saved.PasswordHash, Is.Not.EqualTo(password));
            Assert.That(BCrypt.Net.BCrypt.Verify(password, saved.PasswordHash), Is.True);
        });
    }

    [Test]
    public void CreateUserAsync_WhenEmailAlreadyExists_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = unitOfWork.GetRepository<User>();
        var existing = new User { Id = new Id(Guid.NewGuid()), Email = "test@test.com", PasswordHash = "hash", LastName = "Test", FirstName = "User", ContactInfo = "info" };
        repo.AddRangeAsync([existing], CancellationToken.None).Wait();

        var service = CreateService(unitOfWork);

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            service.CreateUserAsync("test@test.com", "Password123!", "Иванов", "Иван", "", "info", UserRole.Reader));
        Assert.That(ex.ExceptionDetails, Contains.Item("Email уже зарегистрирован"));
    }

    [Test]
    public void CreateUserAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(unitOfWork);

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            service.CreateUserAsync("invalid-email", "123", "", "Иван", "", "info", UserRole.Reader));
        Assert.That(ex.ExceptionDetails, Has.Count.GreaterThan(0));
    }

    [Test]
    public async Task CreateUserAsync_WhenSaveFails_RollsBackTransaction()
    {
        // Arrange — используем реальный UnitOfWork с моком контекста, но проще замокать IUnitOfWork
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockRepo = new Mock<IRepository<User>>();
        mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(mockRepo.Object);
        mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        // Rollback должен быть вызван
        var validator = new UserValidatorAsync(new EmailValidatorAsync());
        var service = new UserService(mockUnitOfWork.Object, validator);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() =>
            service.CreateUserAsync("test@test.com", "Password123!", "Иванов", "Иван", "", "info", UserRole.Reader));

        mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}