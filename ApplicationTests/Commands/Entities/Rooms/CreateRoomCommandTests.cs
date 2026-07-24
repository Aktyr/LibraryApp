namespace LibApp.ApplicationTests.Commands.Entities.Rooms;

[TestFixture]
public class CreateRoomCommandTests
{
    private RoomValidatorAsync CreateRoomValidator => new();
    private IConverter<Room, RoomDTO> Converter => new RoomDTOConverter();


    [TestCase("newRoom")]
    [TestCase("newRoom32")]
    [TestCase("nqweewRoom")]
    [TestCase("newRooqwem")]
    [TestCase("a")]
    [TestCase("1")]
    public async Task Execute_CreateRoomWithNewName_CreatesRoom(string roomName)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        await roomRepo.AddRange(new Bogus.Faker<Room>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.Name, f => f.Name.FirstName())
                                   .Generate(10)
                                   .AsEnumerable());


        var createRoomCommand = new CreateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var createRoomRequest = new CreateRoomRequest(roomName, new List<Guid>());

        // Act
        var response = await createRoomCommand.Execute(createRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That((await roomRepo.Get(x => x.Name == roomName)).Any(), Is.True);
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(11));
        });
    }

    [TestCase("newRoom")]
    [TestCase("newRoom32")]
    [TestCase("nqweewRoom")]
    [TestCase("newRooqwem")]
    [TestCase("a")]
    [TestCase("1")]
    public async Task Execute_CreateRoomWithExistingName_ThrowsRoomExistsException(string roomName)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        await roomRepo.AddRange(new Bogus.Faker<Room>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.Name, f => f.Name.FirstName())
                                   .Generate(10)
                                   .AsEnumerable());
        (await roomRepo.Get()).Last().Name = roomName;

        var createRoomCommand = new CreateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var createRoomRequest = new CreateRoomRequest(roomName, new List<Guid>());

        // Act & Assert
        Assert.ThrowsAsync<RoomExistsException>(() =>
            createRoomCommand.Execute(createRoomRequest, CancellationToken.None));
    }
    [Test]
    public async Task Execute_CreateRoomWithTooLongName_ThrowsValidationException()
    {
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        await roomRepo.AddRange(new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .Generate(10)
            .AsEnumerable());

        var createRoomCommand = new CreateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var createRoomRequest = new CreateRoomRequest(new string('A', 101), new List<Guid>());

        Assert.ThrowsAsync<LibValidationException>(() =>
            createRoomCommand.Execute(createRoomRequest, CancellationToken.None));
    }
}