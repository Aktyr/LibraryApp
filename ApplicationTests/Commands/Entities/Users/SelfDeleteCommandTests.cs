namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class SelfDeleteCommandTests
{
    private static SelfDeleteCommand CreateCommand(
        FakeUnitOfWork uow,
        out Mock<IAuditService> auditMock)
    {
        auditMock = new Mock<IAuditService>();
        return new SelfDeleteCommand(uow, auditMock.Object);
    }

    private static User CreateUser(
        UserRole role = UserRole.Reader,
        bool isDeleted = false,
        ICollection<UserRoomBook>? roomBooks = null)
        => new()
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            Email = "user@library.local",
            ContactInfo = "+7-999-123-45-67",
            PasswordHash = "hash",
            Role = role,
            IsDeleted = isDeleted,
            RoomBooks = roomBooks ?? []
        };

    private static DeleteUserRequest RequestFor(User user) => new() { Id = user.Id };

    [Test]
    public async Task Execute_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _);
        var request = new DeleteUserRequest { Id = new Id(Guid.NewGuid()) };

        var ex = await Assert.ThrowsAsync<UserNotFoundException>(
            () => command.Execute(request, default));

        Assert.That(ex!.Message, Does.Contain("not found").IgnoreCase);
    }

    [Test]
    public async Task Execute_WhenUserIsAdmin_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _);

        var user = CreateUser(role: UserRole.Admin);
        var repo = uow.GetRepository<User>();
        await repo.AddRangeAsync([user], default);

        var ex = await Assert.ThrowsAsync<LibValidationException>(
            () => command.Execute(RequestFor(user), default));

        Assert.That(ex!.Message, Does.Contain("Администратор не может").IgnoreCase);
    }

    [Test]
    public async Task Execute_WhenUserHasActiveBorrows_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _);

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

        var ex = await Assert.ThrowsAsync<LibValidationException>(
            () => command.Execute(RequestFor(user), default));

        Assert.That(ex!.Message, Does.Contain("активных выдач").IgnoreCase);
    }

    [Test]
    public async Task Execute_WhenUserHasUnpaidPenalties_ThrowsLibValidationException()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out _);

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

        var ex = await Assert.ThrowsAsync<LibValidationException>(
            () => command.Execute(RequestFor(user), default));

        Assert.That(ex!.Message, Does.Contain("неоплаченных штрафов").IgnoreCase);
    }

    [Test]
    public async Task Execute_WhenValid_SoftDeletesWithSelfAsDeleterAndLogsAudit()
    {
        var uow = new FakeUnitOfWork();
        var command = CreateCommand(uow, out var auditMock);

        var user = CreateUser();
        var userId = user.Id;
        var userFullName = user.FullName;

        var repo = uow.GetRepository<User>();
        await repo.AddRangeAsync([user], default);

        var response = await command.Execute(RequestFor(user), default);

        Assert.That(response.Status, Is.EqualTo("Ok"));
        Assert.That(response.Message, Does.Contain("деактивирован").IgnoreCase);

        var updatedUser = ((FakeRepository<User>)repo).Entities.First(u => u.Id.Value == userId.Value);
        Assert.That(updatedUser.IsDeleted, Is.True);
        Assert.That(updatedUser.DeletedAt, Is.Not.Null.And.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        // Ключевая проверка: DeletedByUserId указывает на самого пользователя,
        // чтобы /api/auth/restore-account мог отличить self-delete от admin-delete.
        Assert.That(updatedUser.DeletedByUserId, Is.Not.Null);
        Assert.That(updatedUser.DeletedByUserId!.Value, Is.EqualTo(userId.Value));

        auditMock.Verify(a => a.LogAsync(
            AuditActionType.Delete,
            nameof(User),
            userId.Value,
            It.Is<string>(s => s.Contains(userFullName)),
            userId,
            userFullName,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Execute_WhenSaveFails_RollsBackTransaction()
    {
        var uowMock = new Mock<IUnitOfWork>();
        var userRepoMock = new Mock<IRepository<User>>();
        var urbRepoMock = new Mock<IRepository<UserRoomBook>>();
        var auditMock = new Mock<IAuditService>();

        uowMock.Setup(u => u.GetRepository<User>()).Returns(userRepoMock.Object);
        uowMock.Setup(u => u.GetRepository<UserRoomBook>()).Returns(urbRepoMock.Object);

        var user = CreateUser();
        userRepoMock.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        urbRepoMock.SetupSequence(r => r.CountAsync(
                It.IsAny<Expression<Func<UserRoomBook, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)   // активные выдачи
            .ReturnsAsync(0);  // неоплаченные штрафы

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

        var command = new SelfDeleteCommand(uowMock.Object, auditMock.Object);

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
    }
}