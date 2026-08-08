namespace LibApp.ApplicationTests.Helpers;

[TestFixture]
public class ExpressionHelperTests
{
    [Test]
    public void GetPropertyPath_SimpleProperty_ReturnsPath()
    {
        // Arrange
        Expression<Func<Book, string>> expr = b => b.Title;

        // Act
        var path = ExpressionHelper.GetPropertyPath(expr);

        // Assert
        Assert.That(path, Is.EqualTo("Title"));
    }

    [Test]
    public void GetPropertyPath_NestedProperty_ReturnsPath()
    {
        // Arrange
        Expression<Func<RoomBook, int>> expr = rb => rb.Book.Year;

        // Act
        var path = ExpressionHelper.GetPropertyPath(expr);

        // Assert
        Assert.That(path, Is.EqualTo("Book.Year"));
    }

    [Test]
    public void GetPropertyPath_ThrowsOnNonMemberExpression()
    {
        // Arrange
        Expression<Func<Book, bool>> expr = b => b.Title.Contains("test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExpressionHelper.GetPropertyPath(expr));
    }

    [Test]
    public void CombineAnd_CombinesTwoExpressions()
    {
        // Arrange
        Expression<Func<Book, bool>> left = b => b.Year > 2000;
        Expression<Func<Book, bool>> right = b => b.Title.StartsWith('A');

        // Act
        var combined = ExpressionHelper.CombineAnd(left, right);

        // Assert
        var book = new Book { Year = 2005, Title = "A Book" };
        Assert.That(combined.Compile()(book), Is.True);

        book = new Book { Year = 1999, Title = "A Book" };
        Assert.That(combined.Compile()(book), Is.False);
    }
}