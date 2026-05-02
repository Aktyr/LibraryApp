namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class BorrowingValidatorAsyncTests
{
    private BorrowingValidatorAsync CreateValidator(BorrowingSettings? settings = null)
    {
        var optionsMock = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings ?? new BorrowingSettings());
        return new BorrowingValidatorAsync(optionsMock.Object);
    }

    [Test]
    public async Task ValidateBorrowRequestAsync_WithValidData_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator();
        var request = new BorrowBookRequest
        {
            UserId = Guid.NewGuid(),
            RoomBookId = Guid.NewGuid(),
            BorrowDays = 14
        };

        // Act
        var result = await validator.ValidateBorrowRequestAsync(request);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public async Task ValidateBorrowRequestAsync_WhenUserIdIsEmpty_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var request = new BorrowBookRequest
        {
            UserId = Guid.Empty,
            RoomBookId = Guid.NewGuid(),
            BorrowDays = 14
        };

        // Act
        var result = await validator.ValidateBorrowRequestAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("ID пользователя обязателен"));
        });
    }

    [Test]
    public async Task ValidateBorrowRequestAsync_WhenRoomBookIdIsEmpty_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var request = new BorrowBookRequest
        {
            UserId = Guid.NewGuid(),
            RoomBookId = Guid.Empty,
            BorrowDays = 14
        };

        // Act
        var result = await validator.ValidateBorrowRequestAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("ID книги обязателен"));
        });
    }

    [Test]
    public async Task ValidateBorrowRequestAsync_WhenBorrowDaysTooLow_ReturnsError()
    {
        // Arrange
        var settings = new BorrowingSettings { MinBorrowDays = 7, MaxBorrowDays = 30 };
        var validator = CreateValidator(settings);
        var request = new BorrowBookRequest
        {
            UserId = Guid.NewGuid(),
            RoomBookId = Guid.NewGuid(),
            BorrowDays = 3
        };

        // Act
        var result = await validator.ValidateBorrowRequestAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Срок выдачи должен быть от 7 до 30 дней"));
        });
    }

    [Test]
    public async Task ValidateBorrowRequestAsync_WhenBorrowDaysTooHigh_ReturnsError()
    {
        // Arrange
        var settings = new BorrowingSettings { MinBorrowDays = 1, MaxBorrowDays = 14 };
        var validator = CreateValidator(settings);
        var request = new BorrowBookRequest
        {
            UserId = Guid.NewGuid(),
            RoomBookId = Guid.NewGuid(),
            BorrowDays = 30
        };

        // Act
        var result = await validator.ValidateBorrowRequestAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Срок выдачи должен быть от 1 до 14 дней"));
        });
    }

    [Test]
    public async Task ValidateExtendAsync_WhenUserRoomBookIsNull_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateExtendAsync(null!, 7);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Запись о выдаче не найдена"));
        });
    }

    [Test]
    public async Task ValidateReturnAsync_WhenUserRoomBookIsNull_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateReturnAsync(null!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Запись о выдаче не найдена"));
        });
    }
    [Test]
    public async Task ValidateBorrowAsync_WhenUserIsNull_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateBorrowAsync(null!, roomBook, 14);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Пользователь не найден"));
        });
    }

    [Test]
    public async Task ValidateExtendAsync_WhenBookIsAlreadyReturned_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            ReturnDate = new DateTime(2024, 1, 10), // Уже возвращена
            Deadline = new DateTime(2024, 1, 15)
        };

        // Act
        var result = await validator.ValidateExtendAsync(userRoomBook, 7);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Нельзя продлить уже возвращенную книгу"));
        });
    }

    [Test]
    public async Task ValidateExtendAsync_WhenExtraDaysIsZero_ReturnsError()
    {
        // Arrange
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };
        var validator = CreateValidator(settings);
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = new DateTime(2024, 1, 15),
            ReturnDate = null
        };

        // Act
        var result = await validator.ValidateExtendAsync(userRoomBook, 0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Срок продления должен быть от 1 до 14 дней"));
        });
    }

    [Test]
    public async Task ValidateExtendAsync_WhenExtraDaysIsNegative_ReturnsError()
    {
        // Arrange
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };
        var validator = CreateValidator(settings);
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = new DateTime(2024, 1, 15),
            ReturnDate = null
        };

        // Act
        var result = await validator.ValidateExtendAsync(userRoomBook, -5);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Срок продления должен быть от 1 до 14 дней"));
        });
    }

    [Test]
    public async Task ValidateExtendAsync_WhenExtraDaysExceedsMaximum_ReturnsError()
    {
        // Arrange
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };
        var validator = CreateValidator(settings);
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = new DateTime(2024, 1, 15),
            ReturnDate = null
        };

        // Act
        var result = await validator.ValidateExtendAsync(userRoomBook, 20);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Срок продления должен быть от 1 до 14 дней"));
        });
    }
    [Test]
    public async Task ValidateBorrowAsync_WhenRoomBookIsNull_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = new List<UserRoomBook>()
        };

        // Act
        var result = await validator.ValidateBorrowAsync(user, null!, 14);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Книга не найдена"));
        });
    }

    [Test]
    public async Task ValidateBorrowAsync_WhenNoAvailableCopies_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = new List<UserRoomBook>()
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 5, // AvailableCount = 0
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateBorrowAsync(user, roomBook, 14);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Нет доступных экземпляров книги"));
        });
    }
    [Test]
    public async Task ValidateBorrowAsync_WhenUserHasMaxBooks_ReturnsError()
    {
        // Arrange
        var settings = new BorrowingSettings { MaxBooksPerUser = 2, MaxBorrowDays = 30 };
        var validator = CreateValidator(settings);

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = new List<UserRoomBook>
        {
            new UserRoomBook { BorrowDate = DateTime.Now.AddDays(-5), Deadline = DateTime.Now.AddDays(5), ReturnDate = null },
            new UserRoomBook { BorrowDate = DateTime.Now.AddDays(-3), Deadline = DateTime.Now.AddDays(12), ReturnDate = null }
        }
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateBorrowAsync(user, roomBook, 14);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Пользователь уже взял максимальное количество книг ({settings.MaxBooksPerUser})"));
        });
    }

    [Test]
    public async Task ValidateBorrowAsync_WhenUserHasOverdueBooks_ReturnsError()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var roomBookRepo = new FakeRepository<RoomBook>();
        var validator = CreateValidator();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = new List<UserRoomBook>
        {
            new UserRoomBook
            {
                Id = new Id(Guid.NewGuid()),
                BorrowDate = DateTime.Now.AddDays(-30),
                Deadline = DateTime.Now.AddDays(-5), // Просрочена
                ReturnDate = null,
                RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
            }
        }
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateBorrowAsync(user, roomBook, 14);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("У пользователя есть просроченные книги"));
        });
    }

    [Test]
    public async Task ValidateBorrowAsync_WithValidData_ReturnsValid()
    {
        // Arrange
        var validator = CreateValidator();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = new List<UserRoomBook>()
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 2, // AvailableCount = 3 > 0
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateBorrowAsync(user, roomBook, 14);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }


}