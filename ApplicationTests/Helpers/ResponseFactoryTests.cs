namespace LibApp.ApplicationTests.Helpers;

[TestFixture]
public class ResponseFactoryTests
{
    private class FakeResponse : IGetResponse
    {
        public string Status => "";
        public string Message => "";
        // Нет конструктора с (string, string, BookDTO[])
    }

    [Test]
    public void Error_ReturnsErrorResponse()
    {
        // Act
        var response = ResponseFactory.Error("Something went wrong");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Error"));
            Assert.That(response.Message, Is.EqualTo("Something went wrong"));
        });
    }

    [Test]
    public void Empty_ReturnsEmptyResponseWithCorrectMessage()
    {
        // Act
        var response = ResponseFactory.Empty<Book, BookDTO, BookResponse>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("No books found."));
            Assert.That(response.Book, Is.Empty);
        });
    }

    [Test]
    public void CreateResponse_WhenConstructorNotFound_ThrowsInvalidOperationException()
    {
        var method = typeof(ResponseFactory)
            .GetMethod("CreateResponse", BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(typeof(FakeResponse), typeof(BookDTO));

        var ex = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, ["Message", Array.Empty<BookDTO>()]));

        Assert.That(ex.InnerException, Is.InstanceOf<InvalidOperationException>());
    }
}