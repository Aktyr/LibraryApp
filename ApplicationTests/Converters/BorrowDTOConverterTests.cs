namespace LibApp.ApplicationTests.Converters;

[TestFixture]
public class BorrowDTOConverterTests
{
    [Test]
    public void ToEntity_WithValidDto_ThrowsNotSupportedException()
    {
        // Arrange
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(new PenaltySettings());
        var penaltyService = new PenaltyCalculatorService(optionsMock.Object);

        var converter = new BorrowDTOConverter(penaltyService);

        var dto = new BorrowedBookDTO(
            Id: Guid.NewGuid(),
            BookTitle: "Test Book",
            BookAuthor: "Test Author",
            RoomName: "Test Room",
            Borrow: new DateTime(2024, 1, 1),
            Deadline: new DateTime(2024, 1, 15),
            ReturnDate: null,
            Penalty: null
        );

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => converter.ToEntity(dto));
        Assert.That(ex.Message, Is.EqualTo("Конвертация из DTO в сущность не поддерживается"));
    }
}