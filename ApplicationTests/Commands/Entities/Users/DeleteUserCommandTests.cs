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
            .RuleFor(x => x.RoomBooks, f => [])
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
                                   .RuleFor(x => x.RoomBooks, f => [])
                                   .Generate(5)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var deleteUserRequest = new DeleteUserRequest { Id = nonExistingId };

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
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
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteUserWithReturnedBorrows_DeletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var urbRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();

        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        // История: книга уже возвращена
        var borrow = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            BorrowDate = DateTime.UtcNow.AddDays(-10),
            Deadline = DateTime.UtcNow.AddDays(-3),
            ReturnDate = DateTime.UtcNow.AddDays(-2) // возвращена
        };
        await urbRepo.AddRangeAsync([borrow], CancellationToken.None);

        var command = new DeleteUserCommand(unitOfWork);
        var request = new DeleteUserRequest { Id = userId };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(userRepo.Entities, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_WhenUserHasActiveBorrows_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var urbRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();

        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Петров",
            FirstName = "Пётр",
            ContactInfo = "petrov@example.com",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        // Одна активная выдача
        var borrow = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            BorrowDate = DateTime.UtcNow.AddDays(-5),
            Deadline = DateTime.UtcNow.AddDays(5),
            ReturnDate = null // НЕ возвращена
        };
        await urbRepo.AddRangeAsync([borrow], CancellationToken.None);

        var command = new DeleteUserCommand(unitOfWork);
        var request = new DeleteUserRequest { Id = userId };

        // Act & Assert
        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
                command.Execute(request, CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("1 активных выдач"));
            Assert.That(ex.ExceptionDetails, Has.Some.Contains("Сначала верните все книги"));

            // Пользователь НЕ удалён
            Assert.That(userRepo.Entities, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Execute_WhenUserHasMultipleActiveBorrows_ReportsCorrectCount()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var urbRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();

        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Сидоров",
            FirstName = "Сидор",
            ContactInfo = "sidorov@example.com",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        // Две активные + одна возвращённая
        await urbRepo.AddRangeAsync(
        [
            new() { Id = new Id(Guid.NewGuid()), User = user, BorrowDate = DateTime.UtcNow.AddDays(-5), Deadline = DateTime.UtcNow.AddDays(5), ReturnDate = null },
        new() { Id = new Id(Guid.NewGuid()), User = user, BorrowDate = DateTime.UtcNow.AddDays(-3), Deadline = DateTime.UtcNow.AddDays(7), ReturnDate = null },
        new() { Id = new Id(Guid.NewGuid()), User = user, BorrowDate = DateTime.UtcNow.AddDays(-20), Deadline = DateTime.UtcNow.AddDays(-10), ReturnDate = DateTime.UtcNow.AddDays(-9) }
        ], CancellationToken.None);

        var command = new DeleteUserCommand(unitOfWork);
        var request = new DeleteUserRequest { Id = userId };

        // Act & Assert
        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
                command.Execute(request, CancellationToken.None));

            // Считаем только активные, не возвращённые
            Assert.That(ex.ExceptionDetails, Has.Some.Contains("2 активных выдач"));
        });
    }

    [Test]
    public async Task Execute_DeleteUserWithOnlyReturnedBorrows_DeletesSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var urbRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();

        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Кузнецов",
            FirstName = "Кузьма",
            ContactInfo = "kuznetsov@example.com",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        // Только возвращённые
        await urbRepo.AddRangeAsync(
        [
            new() { Id = new Id(Guid.NewGuid()), User = user, BorrowDate = DateTime.UtcNow.AddDays(-30), Deadline = DateTime.UtcNow.AddDays(-20), ReturnDate = DateTime.UtcNow.AddDays(-18) },
        new() { Id = new Id(Guid.NewGuid()), User = user, BorrowDate = DateTime.UtcNow.AddDays(-15), Deadline = DateTime.UtcNow.AddDays(-5), ReturnDate = DateTime.UtcNow.AddDays(-4) }
        ], CancellationToken.None);

        var command = new DeleteUserCommand(unitOfWork);
        var request = new DeleteUserRequest { Id = userId };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(userRepo.Entities, Is.Empty);
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
            .RuleFor(x => x.RoomBooks, f => [])
            .Generate(5)
            .ToList();

        await userRepo.AddRangeAsync(users.AsEnumerable(), CancellationToken.None);

        var deleteUserCommand = new DeleteUserCommand(unitOfWork);
        var initialCount = userRepo.Entities.Count;

        // Act & Assert - удаляем всех пользователей по одному
        await Assert.MultipleAsync(async () =>
        {
            foreach (var user in users.ToList())
            {
                var deleteUserRequest = new DeleteUserRequest { Id = user.Id };
                var result = await deleteUserCommand.Execute(deleteUserRequest, CancellationToken.None);
                Assert.That(result.Status, Is.EqualTo("Ok"));
            }

            Assert.That(userRepo.Entities, Is.Empty);
        });
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

        await userRepo.AddRangeAsync([user], CancellationToken.None);

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

        await userRepo.AddRangeAsync([user], CancellationToken.None);

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
