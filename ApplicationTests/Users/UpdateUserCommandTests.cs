namespace LibApp.ApplicationTests.Users;

[TestFixture]
public class UpdateUserCommandTests
{
    private UserValidatorAsync CreateUserValidator => new();
    private IConverter<User, UserDTO> Converter => new UserDTOConverter();

    [Test]
    public async Task Execute_UpdateExistingUserWithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "old@example.com",
            RoomBooks = []
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Петрович", // Измененное отчество
            ContactInfo = "new@example.com" // Новый контакт
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("User updated successfully."));

            var updatedUser = (await userRepo.Get(x => x.Id.Value == userId.Value)).FirstOrDefault();
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser!.LastName, Is.EqualTo("Иванов"));
            Assert.That(updatedUser.FirstName, Is.EqualTo("Иван"));
            Assert.That(updatedUser.MiddleName, Is.EqualTo("Петрович"));
            Assert.That(updatedUser.ContactInfo, Is.EqualTo("new@example.com"));
            Assert.That(userRepo.Entities, Has.Count.EqualTo(1)); // Количество не изменилось
        });
    }

    [Test]
    public async Task Execute_UpdateNonExistingUser_ThrowsUserNotFoundException()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        await userRepo.AddRange(new Bogus.Faker<User>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.LastName, f => f.Name.LastName())
                                   .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                                   .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
                                   .Generate(5)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = nonExistingId,
            LastName = "Тестов",
            FirstName = "Тест",
            MiddleName = "",
            ContactInfo = "test@test.com"
        };

        // Act & Assert
        Assert.ThrowsAsync<UserNotFoundException>(async () =>
            await updateUserCommand.Execute(updateUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_UpdateUserWithInvalidData_ThrowsValidationException()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "valid@example.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);

        // Пытаемся обновить с пустой фамилией (невалидные данные)
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "", // Пустая фамилия - невалидно
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "valid@example.com"
        };

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () =>
            await updateUserCommand.Execute(updateUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_UpdateUserWithEmptyMiddleName_UpdatesSuccessfully()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "Петрович",
            ContactInfo = "petrov@example.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "", // Пустое отчество допустимо
            ContactInfo = "petrov@example.com"
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUser = (await userRepo.Get(x => x.Id.Value == userId.Value)).FirstOrDefault();
            Assert.That(updatedUser!.MiddleName, Is.EqualTo(""));
        });
    }

    [Test]
    public async Task Execute_UpdateUserPreservesRoomBooksCollection()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Сидорович",
            ContactInfo = "sidorov@example.com",
            RoomBooks = new List<UserRoomBook>
            {
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    Issue = DateTime.Now.AddDays(-5),
                    Deadline = DateTime.Now.AddDays(5)
                }
            }
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Александрович", // Измененное отчество
            ContactInfo = "new.sidorov@example.com"
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUser = (await userRepo.Get(x => x.Id.Value == userId.Value)).FirstOrDefault();
            Assert.That(updatedUser!.RoomBooks, Is.Not.Null);
            Assert.That(updatedUser.RoomBooks, Has.Count.EqualTo(1)); // Коллекция сохранилась
            Assert.That(updatedUser.RoomBooks.First().Deadline, Is.Not.Null);
        });
    }

    [Test]
    public async Task Execute_UpdateUserAllFields_AllFieldsUpdated()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Старый",
            FirstName = "Имя",
            MiddleName = "Отчество",
            ContactInfo = "old@old.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "Новый",
            FirstName = "ДругоеИмя",
            MiddleName = "ДругоеОтчество",
            ContactInfo = "new@new.com"
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUser = (await userRepo.Get(x => x.Id.Value == userId.Value)).FirstOrDefault();
            Assert.That(updatedUser!.LastName, Is.EqualTo("Новый"));
            Assert.That(updatedUser.FirstName, Is.EqualTo("ДругоеИмя"));
            Assert.That(updatedUser.MiddleName, Is.EqualTo("ДругоеОтчество"));
            Assert.That(updatedUser.ContactInfo, Is.EqualTo("new@new.com"));
        });
    }

    [Test]
    public async Task Execute_UpdateUserWithSameData_StillSuccess()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);

        // Обновляем теми же данными
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com"
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.That(response.Status, Is.EqualTo("Ok"));
        Assert.That(response.Message, Is.EqualTo("User updated successfully."));
    }

    [Test]
    public async Task Execute_UpdateUserValidatesBeforeUpdating()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Валидный",
            FirstName = "Пользователь",
            MiddleName = "",
            ContactInfo = "valid@example.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);

        // Слишком длинная фамилия (предполагая, что валидатор проверяет длину)
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = new string('A', 101), // Слишком длинная
            FirstName = "Имя",
            MiddleName = "",
            ContactInfo = "valid@example.com"
        };

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () =>
            await updateUserCommand.Execute(updateUserRequest, CancellationToken.None));

        // Проверяем, что пользователь НЕ был обновлен
        var userAfterFailedUpdate = (await userRepo.Get(x => x.Id.Value == userId.Value)).First();
        Assert.That(userAfterFailedUpdate.LastName, Is.EqualTo("Валидный")); // Осталось прежним
    }

    [TestCase("Иванов", "Иван", "Иванович", "test@test.com")]
    [TestCase("Петрова", "Анна", "", "anna@company.com")]
    [TestCase("Smith", "John", "Doe", "john.smith@mail.com")]
    [TestCase("Сидоров", "Сидор", "Сидорович", "+7-999-123-45-67")]
    public async Task Execute_UpdateUserWithVariousValidData_UpdatesSuccessfully(
        string lastName, string firstName, string middleName, string contactInfo)
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "СтараяФамилия",
            FirstName = "СтароеИмя",
            MiddleName = "СтароеОтчество",
            ContactInfo = "old@old.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            ContactInfo = contactInfo
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUser = (await userRepo.Get(x => x.Id.Value == userId.Value)).FirstOrDefault();
            Assert.That(updatedUser!.LastName, Is.EqualTo(lastName));
            Assert.That(updatedUser.FirstName, Is.EqualTo(firstName));
            Assert.That(updatedUser.MiddleName, Is.EqualTo(middleName));
            Assert.That(updatedUser.ContactInfo, Is.EqualTo(contactInfo));
        });
    }

    [Test]
    public async Task Execute_UpdateUser_PreservesId()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var existingUser = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            ContactInfo = "old@example.com"
        };

        await userRepo.AddRange([existingUser]);

        var updateUserCommand = new UpdateUserCommand(userRepo, CreateUserValidator, Converter);
        var updateUserRequest = new UpdateUserRequest
        {
            Id = userId,
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "",
            ContactInfo = "new@example.com"
        };

        // Act
        var response = await updateUserCommand.Execute(updateUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUser = (await userRepo.Get(x => x.Id.Value == userId.Value)).FirstOrDefault();
            Assert.That(updatedUser!.Id.Value, Is.EqualTo(userId.Value)); // ID не изменился
        });
    }
}