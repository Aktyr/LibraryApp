namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class PenaltyPaymentPolicyTests
{
    [Test]
    public void Validate_WhenPenaltyIsNull_ReturnsError()
    {
        // Act
        var errors = PenaltyPaymentPolicy.Validate(
            penalty: null,
            returnDate: DateTime.UtcNow.AddDays(-1),
            hasExistingPayment: false);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors, Has.Member("Нет штрафа для оплаты"));
        });
    }

    [Test]
    public void Validate_WhenPenaltyIsZero_ReturnsError()
    {
        // Act
        var errors = PenaltyPaymentPolicy.Validate(
            penalty: 0m,
            returnDate: DateTime.UtcNow.AddDays(-1),
            hasExistingPayment: false);

        // Assert
        Assert.That(errors, Has.Member("Нет штрафа для оплаты"));
    }

    [Test]
    public void Validate_WhenBookNotReturned_ReturnsError()
    {
        // Act
        var errors = PenaltyPaymentPolicy.Validate(
            penalty: 100m,
            returnDate: null,
            hasExistingPayment: false);

        // Assert
        Assert.That(errors, Has.Member("Штраф можно оплатить только после возврата книги"));
    }

    [Test]
    public void Validate_WhenAlreadyPaid_ReturnsError()
    {
        // Act
        var errors = PenaltyPaymentPolicy.Validate(
            penalty: 100m,
            returnDate: DateTime.UtcNow.AddDays(-1),
            hasExistingPayment: true);

        // Assert
        Assert.That(errors, Has.Member("Штраф уже оплачен"));
    }

    [Test]
    public void Validate_WithValidData_ReturnsEmptyList()
    {
        // Act
        var errors = PenaltyPaymentPolicy.Validate(
            penalty: 100m,
            returnDate: DateTime.UtcNow.AddDays(-1),
            hasExistingPayment: false);

        // Assert
        Assert.That(errors, Is.Empty);
    }
}