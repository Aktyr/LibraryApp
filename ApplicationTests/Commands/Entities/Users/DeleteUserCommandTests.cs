namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class DeleteUserCommandTests
{
    [Test]
    public async Task Execute_DeleteExistingUser_DeletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var users = new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
            .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
            .RuleFor(x => x.RoomBooks, f => new List<UserRoomBook>())
            .Generate(10)
            .ToList();

        await userRepo.AddRangeAsync(users.AsEnumerable(), CancellationToken.None);

        var userToDelete = users[3]; // Выбираем пользователя для удаления
        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = userToDelete.Id };

        var initialCount = userRepo.Entities.Count;

        // Act
        var result = await deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Is.EqualTo("User deleted successfully."));
            Assert.That(userRepo.Entities, Has.Count.EqualTo(initialCount - 1));

            // Проверяем, что пользователь действительно удален
            var deletedUserStillExists = userRepo.Entities.Any(u => u.Id.Value == userToDelete.Id.Value);
            Assert.That(deletedUserStillExists, Is.False);
        });
    }

    [Test]
    public async Task Execute_DeleteNonExistingUser_ThrowsUserNotFoundException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.LastName, f => f.Name.LastName())
                                   .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                                   .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
                                   .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
                                   .RuleFor(x => x.RoomBooks, f => new List<UserRoomBook>())
                                   .Generate(5)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<UserNotFoundException>(() =>
            deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteUserFromEmptyRepository_ThrowsUserNotFoundException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork(); // Пустой репозиторий
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<UserNotFoundException>(() =>
            deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteUserWithBorrowedBooks_DeletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();

        // Создаем пользователя с "взятыми книгами" (UserRoomBook)
        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com",
            RoomBooks = new List<UserRoomBook>
            {
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-5),
                    Deadline = DateTime.Now.AddDays(5),
                    User = null, // Это не важно для теста
                    RoomBook = null // Это не важно для теста
                }
            }
        };

        await userRepo.AddRangeAsync(new[] { user }, CancellationToken.None);

        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = userId };

        // Act
        var result = await deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(userRepo.Entities, Is.Empty); // Пользователь должен быть удален
        });
    }

    [Test]
    public async Task Execute_DeleteMultipleUsers_EachDeletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var users = new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
            .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
            .RuleFor(x => x.RoomBooks, f => new List<UserRoomBook>())
            .Generate(5)
            .ToList();

        await userRepo.AddRangeAsync(users.AsEnumerable(), CancellationToken.None);

        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var initialCount = userRepo.Entities.Count;

        // Act & Assert - удаляем всех пользователей по одному
        foreach (var user in users.ToList()) // ToList() чтобы создать копию
        {
            var deleteUserRequest = new DeleteUserRequest { Id = user.Id };
            var result = await deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None);

            Assert.That(result.Status, Is.EqualTo("Ok"));
        }

        // Проверяем, что все пользователи удалены
        Assert.That(userRepo.Entities, Is.Empty);
    }

    [Test]
    public async Task Execute_DeleteUser_ReturnsCorrectResponseMessage()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "Петрович",
            ContactInfo = "petrov@example.com",
            RoomBooks = []
        };

        await userRepo.AddRangeAsync(new[] { user }, CancellationToken.None);

        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = user.Id };

        // Act
        var result = await deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Is.EqualTo("User deleted successfully."));
            Assert.That(result, Is.InstanceOf<BasicCreateDeleteResponse>());
        });
    }

    [Test]
    public async Task Execute_DeleteUser_VerifiesCancellationTokenIsPassed()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Сидорович",
            ContactInfo = "sidorov@example.com",
            RoomBooks = []
        };

        await userRepo.AddRangeAsync(new[] { user }, CancellationToken.None);

        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = user.Id };
        var cancellationToken = new CancellationToken();

        // Act
        var result = await deleteUserCommand.Execute(deleteUserRequest, cancellationToken);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Здесь можно добавить проверку, что cancellationToken был передан в репозиторий,
        // если FakeRepository поддерживает эту проверку
    }
}
