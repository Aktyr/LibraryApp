namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class AnonymizeUserCommandTests
{
    private static AnonymizeUserCommand CreateCommand(
        FakeUnitOfWork uow,
        out Mock<IAuditService> auditMock,
        out Mock<ICurrentUserService> currentUserMock)
    {
        auditMock = new Mock<IAuditService>();
        currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(c => c.LibraryUserId).Returns(new Id(Guid.NewGuid()));
        currentUserMock.Setup(c => c.FullName).Returns("Тестовый Администратор");

        return new AnonymizeUserCommand(uow, currentUserMock.Object, auditMock.Object);
    }

    private static User CreateUser(
        bool isDeleted = true,
        UserRole role = UserRole.Reader,
        string email = "user@library.local")
        => new()
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            Email = email,
            ContactInfo = "+7-999-123-45-67",
            PasswordHash = "hash",
            Role = role,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            DeletedByUserId = isDeleted ? new Id(Guid.NewGuid()) : null,
            RoomBooks = []
        };

    private static DeleteUserRequest RequestFor(User user) => new() { Id = user.Id };

    [Test]
    public async Task Execute_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _, out _);
        var request = new DeleteUserRequest { Id = new Id(Guid.NewGuid()) };

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<UserNotFoundException>(
                () => command.Execute(request, default));

            Assert.That(ex!.Message, Does.Contain("not found").IgnoreCase);
        });
    }

    [Test]
    public async Task Execute_WhenUserIsAdmin_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _, out _);

        var user = CreateUser(role: UserRole.Admin);
        var repo = uow.GetRepository<User>();
        await repo.AddRangeAsync([user], default);

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(
                () => command.Execute(RequestFor(user), default));

            Assert.That(ex!.Message, Does.Contain("администратора").IgnoreCase);
        });
    }

    [Test]
    public async Task Execute_WhenUserIsNotDeleted_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _, out _);

        var user = CreateUser(isDeleted: false);
        var repo = uow.GetRepository<User>();
        await repo.AddRangeAsync([user], default);

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(
                () => command.Execute(RequestFor(user), default));

            Assert.That(ex!.Message, Does.Contain("только для удалённых").IgnoreCase);
        });
    }

    [Test]
    public async Task Execute_WhenUserHasActiveBorrows_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _, out _);

        var user = CreateUser();
        var userRepo = uow.GetRepository<User>();
        var urbRepo = uow.GetRepository<UserRoomBook>();
        await userRepo.AddRangeAsync([user], default);

        var borrow = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            ReturnDate = null,
            RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
        };
        await urbRepo.AddRangeAsync([borrow], default);

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(
                () => command.Execute(RequestFor(user), default));

            Assert.That(ex!.Message, Does.Contain("активных выдач").IgnoreCase);
        });
    }

    [Test]
    public async Task Execute_WhenUserHasUnpaidPenalties_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _, out _);

        var user = CreateUser();
        var userRepo = uow.GetRepository<User>();
        var urbRepo = uow.GetRepository<UserRoomBook>();
        await userRepo.AddRangeAsync([user], default);

        var borrow = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            ReturnDate = DateTime.UtcNow,
            Penalty = 100m,
            PenaltyPayments = [],
            RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
        };
        await urbRepo.AddRangeAsync([borrow], default);

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(
                () => command.Execute(RequestFor(user), default));

            Assert.That(ex!.Message, Does.Contain("неоплаченных штрафов").IgnoreCase);
        });
    }

    [Test]
    public async Task Execute_WhenAlreadyAnonymized_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _, out _);

        var user = CreateUser(email: "deleted_0123456789abcdef@library.local");
        var repo = uow.GetRepository<User>();
        await repo.AddRangeAsync([user], default);

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(
                () => command.Execute(RequestFor(user), default));

            Assert.That(ex!.Message, Does.Contain("уже анонимизирован").IgnoreCase);
        });
    }

    [Test]
    public async Task Execute_WhenValid_AnonymizesAndLogsAudit()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out var auditMock, out _);

        var user = CreateUser();
        var originalId = user.Id;
        var originalEmail = user.Email;
        var originalFullName = user.FullName;
        var originalDeletedAt = user.DeletedAt;
        var originalDeletedBy = user.DeletedByUserId;

        var repo = uow.GetRepository<User>();
        await repo.AddRangeAsync([user], default);

        await Assert.MultipleAsync(async () =>
        {
            var response = await command.Execute(RequestFor(user), default);

            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("анонимизирован").IgnoreCase);

            var updatedUser = ((FakeRepository<User>)repo).Entities
                .First(u => u.Id.Value == originalId.Value);
            Assert.That(updatedUser.LastName, Is.EqualTo("Deleted"));
            Assert.That(updatedUser.FirstName, Is.EqualTo("User"));
            Assert.That(updatedUser.MiddleName, Is.Empty);
            Assert.That(updatedUser.Email, Is.EqualTo($"deleted_{originalId.Value:N}@library.local"));
            Assert.That(updatedUser.ContactInfo, Is.Empty);
            Assert.That(updatedUser.PasswordHash, Is.Empty);
            Assert.That(updatedUser.IsDeleted, Is.True);
            Assert.That(updatedUser.DeletedAt, Is.EqualTo(originalDeletedAt));
            Assert.That(updatedUser.DeletedByUserId, Is.EqualTo(originalDeletedBy));

            auditMock.Verify(a => a.LogAsync(
                AuditActionType.Anonymize,
                nameof(User),
                originalId.Value,
                It.Is<string>(s => s.Contains(originalFullName) && s.Contains(originalEmail)),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        });
    }

    [Test]
    public async Task Execute_WhenSaveFails_RollsBackTransaction()
    {
        var uowMock = new Mock<IUnitOfWork>();
        var userRepoMock = new Mock<IRepository<User>>();
        var urbRepoMock = new Mock<IRepository<UserRoomBook>>();
        var auditMock = new Mock<IAuditService>();
        var currentUserMock = new Mock<ICurrentUserService>();

        currentUserMock.Setup(c => c.LibraryUserId).Returns(new Id(Guid.NewGuid()));
        currentUserMock.Setup(c => c.FullName).Returns("Тестовый Администратор");

        uowMock.Setup(u => u.GetRepository<User>()).Returns(userRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<UserRoomBook>()).Returns(urbRepoMock.Object);

        var user = CreateUser();

        userRepoMock.Setup(r => r.ExecuteQueryIgnoringFiltersAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });

        urbRepoMock.SetupSequence(r => r.CountAsync(
                It.IsAny<Expression<Func<UserRoomBook, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .ReturnsAsync(0);

        var saveException = new InvalidOperationException("db down");
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(saveException);

        uowMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, ct) =>
            {
                await uowMock.Object.BeginTransactionAsync(ct);
                try
                {
                    await action();
                    await uowMock.Object.CommitTransactionAsync(ct);
                }
                catch
                {
                    await uowMock.Object.RollbackTransactionAsync(ct);
                    throw;
                }
            });

        uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new AnonymizeUserCommand(uowMock.Object, currentUserMock.Object, auditMock.Object);

        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => command.Execute(RequestFor(user), default));

            Assert.That(ex, Is.SameAs(saveException));

            uowMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

            auditMock.Verify(a => a.LogAsync(
                It.IsAny<AuditActionType>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<Id?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        });
    }
}