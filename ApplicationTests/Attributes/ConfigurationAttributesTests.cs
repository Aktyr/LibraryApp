namespace LibApp.ApplicationTests.Attributes;

[TestFixture]
public class MaxRangeAttributeTests
{
    [Test]
    public void IsValid_WhenValueIsNull_ReturnsSuccess()
    {
        // Arrange
        var attr = new MaxRangeAttribute(100);
        var context = new ValidationContext(new object());

        // Act
        var result = attr.GetValidationResult(null, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueWithinRange_ReturnsSuccess()
    {
        // Arrange
        var attr = new MaxRangeAttribute(100);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(50, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueExactlyAtMax_ReturnsSuccess()
    {
        // Arrange
        var attr = new MaxRangeAttribute(100);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(100, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueExceedsMax_ReturnsError()
    {
        // Arrange
        var attr = new MaxRangeAttribute(100);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(150, context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
            Assert.That(result!.ErrorMessage, Does.Contain("cannot exceed 100"));
        });
    }

    [Test]
    public void IsValid_WithDoubleValue_ConvertsCorrectly()
    {
        // Arrange
        var attr = new MaxRangeAttribute(10.5);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(10.6, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }
}

[TestFixture]
public class MinRangeAttributeTests
{
    [Test]
    public void IsValid_WhenValueIsNull_ReturnsSuccess()
    {
        // Arrange
        var attr = new MinRangeAttribute(0);
        var context = new ValidationContext(new object());

        // Act
        var result = attr.GetValidationResult(null, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueWithinRange_ReturnsSuccess()
    {
        // Arrange
        var attr = new MinRangeAttribute(10);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(50, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueExactlyAtMin_ReturnsSuccess()
    {
        // Arrange
        var attr = new MinRangeAttribute(10);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(10, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueBelowMin_ReturnsError()
    {
        // Arrange
        var attr = new MinRangeAttribute(10);
        var context = new ValidationContext(new object()) { DisplayName = "TestProperty" };

        // Act
        var result = attr.GetValidationResult(5, context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
            Assert.That(result!.ErrorMessage, Does.Contain("must be at least 10"));
        });
    }
}

[TestFixture]
public class PositiveNumberAttributeTests
{
    [Test]
    public void IsValid_WhenValueIsNull_ReturnsSuccess()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attr.GetValidationResult(null, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueIsPositive_ReturnsSuccess()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Amount" };

        // Act
        var result = attr.GetValidationResult(5, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueIsZero_ReturnsError()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Amount" };

        // Act
        var result = attr.GetValidationResult(0, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueIsNegative_ReturnsError()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Amount" };

        // Act
        var result = attr.GetValidationResult(-5, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WithDecimalValue_ConvertsCorrectly()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Amount" };

        // Act
        var result = attr.GetValidationResult(0.01m, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenValueIsNonNumericString_ThrowsFormatException()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Amount" };

        // Act & Assert
        Assert.Throws<FormatException>(() => attr.GetValidationResult("not a number", context));
    }

    [Test]
    public void IsValid_WhenValueIsIncompatibleType_ReturnsSuccess()
    {
        // Arrange
        var attr = new PositiveNumberAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Amount" };

        // Act — тип, который нельзя сконвертировать в decimal через Convert.ToDecimal,
        // но который не вызовет FormatException а InvalidCastException
        var result = attr.GetValidationResult(new object(), context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }
}

[TestFixture]
public class ValidEmailAttributeTests
{
    [Test]
    public void IsValid_WhenValueIsNull_ReturnsSuccess()
    {
        // Arrange
        var attr = new ValidEmailAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attr.GetValidationResult(null, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenEmailIsValid_ReturnsSuccess()
    {
        // Arrange
        var attr = new ValidEmailAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Email" };

        // Act
        var result = attr.GetValidationResult("test@example.com", context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenEmailIsEmpty_ReturnsSuccess()
    {
        // Arrange
        var attr = new ValidEmailAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Email" };

        // Act
        var result = attr.GetValidationResult("", context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenEmailIsWhitespace_ReturnsSuccess()
    {
        // Arrange
        var attr = new ValidEmailAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Email" };

        // Act
        var result = attr.GetValidationResult("   ", context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenEmailIsInvalid_ReturnsError()
    {
        // Arrange
        var attr = new ValidEmailAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Email" };

        // Act
        var result = attr.GetValidationResult("not-an-email", context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }
}

[TestFixture]
public class MinMaxValidatorAttributeTests
{
    [Test]
    public void IsValid_WhenMinLessThanMax_ReturnsSuccess()
    {
        // Arrange
        var attr = new MinMaxValidatorAttribute("MinBorrowDays", "MaxBorrowDays");
        var settings = new BorrowingSettings
        {
            MinBorrowDays = 1,
            MaxBorrowDays = 30
        };
        var context = new ValidationContext(settings);

        // Act
        var result = attr.GetValidationResult(settings, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenMinGreaterThanMax_ReturnsError()
    {
        // Arrange
        var attr = new MinMaxValidatorAttribute("MinBorrowDays", "MaxBorrowDays");
        var settings = new BorrowingSettings
        {
            MinBorrowDays = 50,
            MaxBorrowDays = 30
        };
        var context = new ValidationContext(settings);

        // Act
        var result = attr.GetValidationResult(settings, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenMinEqualsMax_ReturnsError()
    {
        // Arrange
        var attr = new MinMaxValidatorAttribute("MinBorrowDays", "MaxBorrowDays");
        var settings = new BorrowingSettings
        {
            MinBorrowDays = 30,
            MaxBorrowDays = 30
        };
        var context = new ValidationContext(settings);

        // Act
        var result = attr.GetValidationResult(settings, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenPropertyNotFound_ReturnsError()
    {
        // Arrange
        var attr = new MinMaxValidatorAttribute("NonExistentMin", "MaxBorrowDays");
        var settings = new BorrowingSettings
        {
            MinBorrowDays = 1,
            MaxBorrowDays = 30
        };
        var context = new ValidationContext(settings);

        // Act
        var result = attr.GetValidationResult(settings, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenPropertyTypesMismatch_ReturnsError()
    {
        // Arrange — используем класс с разными типами для проверки этого сценария
        var attr = new MinMaxValidatorAttribute("IntProperty", "DecimalProperty");
        var obj = new { IntProperty = 5, DecimalProperty = 10m };
        var context = new ValidationContext(obj);

        // Act
        var result = attr.GetValidationResult(obj, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }
}