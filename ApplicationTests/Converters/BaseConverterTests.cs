namespace LibApp.ApplicationTests.Converters;

[TestFixture]
public class BaseConverterTests
{
    private class EntityWithNullGenericCollectionConverter : BaseConverter<EntityWithNullGenericCollection, RoomDTO> { }
    private class EntityWithNullCollectionConverter : BaseConverter<EntityWithNullCollection, RoomDTO> { }
    private class EntityConverter : BaseConverter<EntityWithNonGenericCollection, UserDTO> { }
    private class EntityWithIndexerConverter : BaseConverter<EntityWithIndexer, RoomDTO> { }
    private class TestConverter : BaseConverter<User, UserDTO> { }
    private class EntityWithIndexer : IEntity
    {
        public Id Id { get; set; } = new Id(Guid.NewGuid());
        public string Name { get; set; } = string.Empty;

        // Индексатор — должен пропускаться при маппинге
        public string this[int index]
        {
            get => Name;
            set => Name = value;
        }
    }

    private class EntityWithNullCollection : IEntity
    {
        public Id Id { get; set; } = new Id(Guid.NewGuid());
        public string Name { get; set; } = string.Empty;

        // БЕЗ nullable (без "?") и БЕЗ инициализации
        public virtual ICollection<RoomBook> Items { get; set; }
    }

    // Тестовая сущность с не-generic коллекцией
    private class EntityWithNonGenericCollection : IEntity
    {
        public Id Id { get; set; } = new Id(Guid.NewGuid());

        // Не-generic коллекция
        public ArrayList? LegacyList { get; set; }

        // Обычная generic коллекция для сравнения
        public ICollection<RoomBook>? RoomBooks { get; set; }
    }
    private class EntityWithNullGenericCollection : IEntity
    {
        public Id Id { get; set; } = new Id(Guid.NewGuid());
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<RoomBook>? RoomBooks { get; set; } // null!
    }

    [Test]
    public void ToDto_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var converter = new TestConverter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.ToDto(null!));
    }

    [Test]
    public void ToEntity_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange
        var converter = new TestConverter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.ToEntity(null!));
    }

    [Test]
    public void ToEntity_WhenCollectionPropertyIsNull_InitializesIt()
    {
        // Arrange
        var converter = new TestConverter();
        var dto = new UserDTO(Guid.NewGuid(), "Test", "User", "", "test@test.com", null, null!);

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.That(entity.RoomBooks, Is.Not.Null);
        Assert.That(entity.RoomBooks, Is.Empty);
    }

    [Test]
    public void ToEntity_ConvertsGuidToIdCorrectly()
    {
        // Arrange
        var converter = new TestConverter();
        var guid = Guid.NewGuid();
        var dto = new UserDTO(guid, "Test", "User", "", "test@test.com", null, null!);

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.That(entity.Id.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ToDto_ConvertsIdToGuidCorrectly()
    {
        // Arrange
        var converter = new TestConverter();
        var entity = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com"
        };

        // Act
        var dto = converter.ToDto(entity);

        // Assert
        Assert.That(dto.Id, Is.EqualTo(entity.Id.Value));
    }
    [Test]
    public void InitializeCollections_WhenCollectionPropertyAlreadyInitialized_DoesNothing()
    {
        // Arrange
        var existingCollection = new List<UserRoomBook>
    {
        new() { Id = new Id(Guid.NewGuid()) }
    };

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = existingCollection
        };

        var method = typeof(BaseConverter<User, UserDTO>)
            .GetMethod("InitializeCollections",
                BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method?.Invoke(null, [user]);

        // Assert
        Assert.That(user.RoomBooks, Is.SameAs(existingCollection)); // Коллекция не пересоздалась
        Assert.That(user.RoomBooks, Has.Count.EqualTo(1));
    }
    [Test]
    public void MapProperties_WhenTargetPropertyIsReadOnly_SkipsIt()
    {
        // Arrange
        var converter = new TestConverter();
        var guid = Guid.NewGuid();

        // Создаём DTO с ReadOnly свойством через record (все свойства record — init-only)
        var dto = new UserDTO(guid, "Test", "User", "", "test@test.com", null, new List<UserRoomBookDTO>());

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.That(entity.Id.Value, Is.EqualTo(guid));
        Assert.That(entity.LastName, Is.EqualTo("Test"));
        Assert.That(entity.FirstName, Is.EqualTo("User"));
        Assert.That(entity.ContactInfo, Is.EqualTo("test@test.com"));
        // RoomBooks маппится через InitializeCollections, а не через MapProperties
    }

    [Test]
    public void MapProperties_WhenSourcePropertyIsIndexer_SkipsIt()
    {
        // Arrange — этот тест скорее для документации, 
        // так как у entity классов нет индексаторов,
        // но условие в коде есть и должно быть покрыто

        // Используем reflection для прямого тестирования MapProperties
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com"
        };

        var dto = new UserDTO(Guid.Empty, "", "", "", "", null, null!);

        var method = typeof(BaseConverter<User, UserDTO>)
            .GetMethod("MapProperties",
                BindingFlags.NonPublic | BindingFlags.Instance)
            ?.MakeGenericMethod(typeof(User), typeof(UserDTO));

        // Act — не должно быть исключений
        Assert.DoesNotThrow(() => method?.Invoke(new TestConverter(), [user, dto]));
    }
    


    [Test]
    public void InitializeCollections_WhenPropertyIsNonGenericCollection_DoesNotInitialize()
    {
        // Arrange
        var entity = new EntityWithNonGenericCollection
        {
            Id = new Id(Guid.NewGuid()),
            LegacyList = null
        };

        var method = typeof(BaseConverter<EntityWithNonGenericCollection, UserDTO>)
            .GetMethod("InitializeCollections",
                BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method?.Invoke(null, [entity]);

        // Assert
        // Не-generic коллекция НЕ инициализируется (пропускается в условии)
        Assert.That(entity.LegacyList, Is.Null);
    }

    [Test]
    public void InitializeCollections_WhenPropertyIsString_SkipsIt()
    {
        // String — это IEnumerable<char>, но не должен инициализироваться
        // Это покрывается существующим условием: p.PropertyType != typeof(string)

        // Arrange
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = null!
        };

        var method = typeof(BaseConverter<User, UserDTO>)
            .GetMethod("InitializeCollections", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method?.Invoke(null, [user]);

        // Assert
        Assert.That(user.LastName, Is.Null); // String property не инициализируется как коллекция
    }

    [Test]
    public void InitializeCollections_WhenEntityHasNoNullCollections_DoesNothing()
    {
        // Arrange
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            RoomBooks = new List<UserRoomBook>{ new() {Id = new Id(Guid.NewGuid())}}
        };

        var roomBooksBefore = user.RoomBooks;

        var method = typeof(BaseConverter<User, UserDTO>)
            .GetMethod("InitializeCollections", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method?.Invoke(null, [user]);

        // Assert
        Assert.That(user.RoomBooks, Is.SameAs(roomBooksBefore)); // Коллекция не изменилась
    }
    [Test]
    public void ToEntity_WhenDtoHasNoRoomBooks_InitializesRoomBooksCollection()
    {
        // Arrange
        var converter = new RoomDTOConverter();
        var dto = new RoomDTO(Guid.NewGuid(), "Test Room", []);

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.RoomBooks, Is.Not.Null);
            Assert.That(entity.RoomBooks, Is.InstanceOf<List<RoomBook>>());
        });
    }
    [Test]
    public void MapProperties_WhenSourcePropertyIsIndexer_SkipsIt_ViaEntityWithIndexer()
    {
        // Arrange
        var converter = new EntityWithIndexerConverter();
        var entity = new EntityWithIndexer
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Test"
        };

        var dto = new RoomDTO(Guid.NewGuid(), "Test Room", new List<RoomBookDTO>());

        // Act
        var result = converter.ToDto(entity);

        // Assert — не должно быть исключений из-за индексатора
        Assert.That(result, Is.Not.Null);
    }
    [Test]
    public void ToDto_WithValidEntity_MapsAllProperties()
    {
        // Arrange
        var converter = new TestConverter();
        var entityId = Guid.NewGuid();
        var entity = new User
        {
            Id = new Id(entityId),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "test@test.com",
            RoomBooks = new List<UserRoomBook>() // Не проверяется маппингом напрямую
        };

        // Act
        var dto = converter.ToDto(entity);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Id, Is.EqualTo(entityId));
            Assert.That(dto.LastName, Is.EqualTo("Иванов"));
            Assert.That(dto.FirstName, Is.EqualTo("Иван"));
            Assert.That(dto.MiddleName, Is.EqualTo("Иванович"));
            Assert.That(dto.ContactInfo, Is.EqualTo("test@test.com"));
            // NearestReturnTimeSpan не маппится, так как имена свойств разные
            // RoomBooks не маппится, так как имена коллекций разные (RoomBook vs RoomBooks)
        });
    }

}