namespace LibApp.ApplicationTests.DTO;

[TestFixture]
public class DtoConstructorTests
{
    [Test]
    public void BookPopularityReportResponse_Constructor_SetsProperties()
    {
        // Arrange
        var status = "Ok";
        var message = "Test";
        var data = new BookPopularityReportDTO[] { new(Guid.NewGuid(), "Title", "Author", 5, 2) };

        // Act
        var response = new BookPopularityReportResponse(status, message, data);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(status));
            Assert.That(response.Message, Is.EqualTo(message));
            Assert.That(response.Data, Is.SameAs(data));
        });
    }

    [Test]
    public void GetPopularBooksRequest_DefaultConstructor_InitializesTopCount()
    {
        // Act
        var request = new GetPopularBooksRequest();

        // Assert
        Assert.That(request.TopCount, Is.EqualTo(10));
    }

    [Test]
    public void DiscardedBookDTO_DefaultConstructor_SetsDefaults()
    {
        // Act
        var dto = new DiscardedBookDTO();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(Guid.Empty));
            Assert.That(dto.BookId, Is.EqualTo(Guid.Empty));
            Assert.That(dto.BookTitle, Is.Null);
            Assert.That(dto.Amount, Is.EqualTo(0));
            Assert.That(dto.DiscardedDate, Is.EqualTo(default(DateTime)));
            Assert.That(dto.DiscardReason, Is.EqualTo(default(DiscardReason)));
            Assert.That(dto.ApprovedBy, Is.Null);
            Assert.That(dto.CompensationAmount, Is.Null);
        });
    }

    [Test]
    public void BookPopularityReportDTO_Constructor_SetsProperties()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var title = "Test Book";
        var author = "Test Author";
        var totalBorrowed = 10;
        var currentBorrowed = 3;

        // Act
        var dto = new BookPopularityReportDTO(bookId, title, author, totalBorrowed, currentBorrowed);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dto.BookId, Is.EqualTo(bookId));
            Assert.That(dto.Title, Is.EqualTo(title));
            Assert.That(dto.Author, Is.EqualTo(author));
            Assert.That(dto.TotalBorrowedCount, Is.EqualTo(totalBorrowed));
            Assert.That(dto.CurrentBorrowedCount, Is.EqualTo(currentBorrowed));
        });
    }
}