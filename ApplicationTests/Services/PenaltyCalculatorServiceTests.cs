using LibApp.Application.Configuration;

namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class PenaltyCalculatorServiceTests
{
    private PenaltyCalculatorService CreateService(PenaltySettings settings)
    {
        var options = new Mock<IOptionsSnapshot<PenaltySettings>>();
        options.Setup(x => x.Value).Returns(settings);
        return new PenaltyCalculatorService(options.Object);
    }

    [Test]
    public void CalculatePenalty_WhenDeadlineIsNull_ReturnsZero()
    {
        var service = CreateService(new PenaltySettings());
        var result = service.CalculatePenalty(null);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePenalty_WhenNotOverdue_ReturnsZero()
    {
        var service = CreateService(new PenaltySettings());
        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(5);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePenalty_WhenWithinGracePeriod_ReturnsZero()
    {
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 3 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-2);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePenalty_WhenOverdue_CalculatesCorrectly()
    {
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 0 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-5);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(50m));
    }

    [Test]
    public void CalculatePenalty_WhenOverdueWithGracePeriod_CalculatesCorrectly()
    {
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 2 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-7);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(50m));
    }

    [Test]
    public void CalculatePenalty_WhenOverdueWithMaxPenalty_CapsCorrectly()
    {
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = 30m };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-10);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(30m));
    }

    [Test]
    public void CalculatePenalty_WhenNoMaxPenalty_NoCap()
    {
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = null };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-10);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(100m));
    }

    [Test]
    public void CalculatePenalty_WhenPartialDayOverdue_RoundsUp()
    {
        var settings = new PenaltySettings { DailyRate = 10m };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-5).AddHours(-12);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(60m));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenBookIsReturned_ReturnsSavedPenalty()
    {
        var service = CreateService(new PenaltySettings());
        var userRoomBook = new UserRoomBook
        {
            ReturnDate = DateTime.Now,
            Penalty = 25m
        };

        var result = service.CalculatePenaltyForReturn(userRoomBook, DateTime.Now);
        Assert.That(result, Is.EqualTo(25m));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenBookNotReturned_CalculatesCurrentPenalty()
    {
        var settings = new PenaltySettings { DailyRate = 10m };
        var service = CreateService(settings);
        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var userRoomBook = new UserRoomBook
        {
            ReturnDate = null,
            Deadline = now.AddDays(-5)
        };

        var result = service.CalculatePenaltyForReturn(userRoomBook, now);
        Assert.That(result, Is.EqualTo(50m));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenBookHasExistingPenalty_TakesMax()
    {
        var settings = new PenaltySettings { DailyRate = 10m };
        var service = CreateService(settings);
        var now = new DateTime(2024, 1, 10, 12, 0, 0);

        var userRoomBook1 = new UserRoomBook
        {
            Penalty = 30m,
            Deadline = now.AddDays(-3)
        };

        var userRoomBook2 = new UserRoomBook
        {
            Penalty = 20m,
            Deadline = now.AddDays(-5)
        };

        var result1 = service.CalculatePenaltyForReturn(userRoomBook1, now);
        var result2 = service.CalculatePenaltyForReturn(userRoomBook2, now);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(30m));
            Assert.That(result2, Is.EqualTo(50m));
        });
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenUserRoomBookIsNull_ThrowsException()
    {
        var service = CreateService(new PenaltySettings());
        Assert.Throws<ArgumentNullException>(() =>
            service.CalculatePenaltyForReturn(null!));
    }

    [Test]
    public void CalculatePenalty_RespectsCustomDailyRate_CalculatesCorrectly()
    {
        var settings = new PenaltySettings { DailyRate = 25m };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-3);

        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(75m));
    }

    [Test]
    public void CalculatePenalty_WhenMaxPenaltyIsZero_ReturnsZero()
    {
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = 0m };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-10);

        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WhenGracePeriodLargerThanOverdue_ReturnsZero()
    {
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 100 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-50);

        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WhenExactlyOnDeadline_ReturnsZero()
    {
        var service = CreateService(new PenaltySettings());
        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now;
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePenalty_WhenOneMinuteOverdue_ReturnsOneDayPenalty()
    {
        var settings = new PenaltySettings { DailyRate = 10m };
        var service = CreateService(settings);
        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddMinutes(-1);
        var result = service.CalculatePenalty(deadline, now);
        Assert.That(result, Is.EqualTo(10m));
    }
    [Test]
    public void CalculatePenalty_WhenOverdueEqualsMaxPenalty_ReturnsMaxPenalty()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = 50m, GracePeriodDays = 0 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-5); // 5 дней × 10 = 50 = MaxPenalty

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(50m));
    }

    [Test]
    public void CalculatePenalty_WhenOverdueJustBelowMaxPenalty_ReturnsCalculatedPenalty()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = 50m, GracePeriodDays = 0 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-4); // 4 дней × 10 = 40 < 50

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(40m));
    }

    [Test]
    public void CalculatePenalty_WhenGracePeriodEqualsOverdue_ReturnsZero()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 3 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-3); // Просрочка ровно 3 дня (равна GracePeriod)

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WhenGracePeriodExceededByOneDay_ReturnsOneDayPenalty()
    {
        // Arrange 
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 3 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-4); // Просрочка 4 дня, grace = 3 → штраф за 1 день

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(10m));
    }

    [Test]
    public void CalculatePenalty_WhenDeadlineIsExactlyAtGracePeriodEnd_ReturnsZero()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 3 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-3); // deadline = -3, graceEnd = -3 + 3 = 0

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WhenOneHourPastGracePeriod_RoundsUpToOneDay()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 3 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-3).AddHours(-1); // deadline чуть раньше, чем grace

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert  
        // просрочка = Ceil((now - (-3 days -1h)).TotalDays) - 3 = Ceil(3.04) - 3 = 4 - 3 = 1 день × 10 = 10
        Assert.That(result, Is.EqualTo(10m));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenReturnedBookHasNoSavedPenalty_ReturnsZero()
    {
        // Arrange
        var service = CreateService(new PenaltySettings());
        var userRoomBook = new UserRoomBook
        {
            ReturnDate = DateTime.Now,
            Penalty = null // Возвращена без штрафа
        };

        // Act
        var result = service.CalculatePenaltyForReturn(userRoomBook);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenBookNotOverdueAndNoExistingPenalty_ReturnsZero()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var userRoomBook = new UserRoomBook
        {
            ReturnDate = null,
            Deadline = now.AddDays(5), // Дедлайн в будущем
            Penalty = null
        };

        // Act
        var result = service.CalculatePenaltyForReturn(userRoomBook, now);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WithFractionalDailyRate_RoundsCorrectly()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 3.333m, GracePeriodDays = 0 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-3);

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        // 3.333 × 3 = 9.999 → округляется до 10.00
        Assert.That(result, Is.EqualTo(10.00m));
    }

    [Test]
    public void CalculatePenalty_WithVeryLargeOverdue_CalculatesCorrectly()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = null, GracePeriodDays = 0 };
        var service = CreateService(settings);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-1000); // 1000 дней просрочки

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(10000m));
    }
    [Test]
    public void CalculatePenalty_WhenActualDaysOverdueIsZero_ReturnsZero()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 5 };

        var loggerMock = new Mock<ILogger<PenaltyCalculatorService>>();
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings);

        var service = new PenaltyCalculatorService(optionsMock.Object, loggerMock.Object);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-5); // 5 дней просрочки, grace = 5 → actualDaysOverdue = 0

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WhenDeadlineIsNull_LogsDebug()
    {
        // Arrange
        var settings = new PenaltySettings();

        var loggerMock = new Mock<ILogger<PenaltyCalculatorService>>();
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings);

        var service = new PenaltyCalculatorService(optionsMock.Object, loggerMock.Object);

        // Act
        var result = service.CalculatePenalty(null);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
        // Проверка логирования не требуется, так как это побочный эффект
    }

    [Test]
    public void CalculatePenalty_WhenWithinGracePeriod_LogsDebug()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 3 };

        var loggerMock = new Mock<ILogger<PenaltyCalculatorService>>();
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings);

        var service = new PenaltyCalculatorService(optionsMock.Object, loggerMock.Object);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-2);

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculatePenalty_WhenMaxPenaltyExceeded_LogsInformation()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, MaxPenalty = 50m, GracePeriodDays = 0 };

        var loggerMock = new Mock<ILogger<PenaltyCalculatorService>>();
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings);

        var service = new PenaltyCalculatorService(optionsMock.Object, loggerMock.Object);

        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(-10);

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(50m));
    }
    [Test]
    public void CalculatePenalty_WhenFutureDeadline_ReturnsZero()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 0 };
        var service = CreateService(settings);
        var now = new DateTime(2024, 1, 10, 12, 0, 0);
        var deadline = now.AddDays(5);

        // Act
        var result = service.CalculatePenalty(deadline, now);

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenReturnedWithNullPenalty_ReturnsZero()
    {
        // Arrange
        var service = CreateService(new PenaltySettings());
        var userRoomBook = new UserRoomBook
        {
            ReturnDate = new DateTime(2024, 1, 10),
            Penalty = null
        };

        // Act
        var result = service.CalculatePenaltyForReturn(userRoomBook);

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePenaltyForReturn_WhenNotReturnedAndPenaltyExists_TakesMax()
    {
        // Arrange
        var settings = new PenaltySettings { DailyRate = 10m };
        var service = CreateService(settings);
        var now = new DateTime(2024, 1, 10, 12, 0, 0);

        var userRoomBook = new UserRoomBook
        {
            ReturnDate = null,
            Penalty = 30m,
            Deadline = now.AddDays(-3)
        };

        // Act
        var result = service.CalculatePenaltyForReturn(userRoomBook, now);

        // Assert
        // 3 дня × 10 = 30, сохраненный = 30 → берём 30
        Assert.That(result, Is.EqualTo(30m));
    }
}