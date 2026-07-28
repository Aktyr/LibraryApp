namespace LibApp.Application.Services;

public class RoomBookSynchronizer : IRoomBookSynchronizer, IService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomBookSynchronizer> _logger;
    private readonly IRoomBookSynchronizationValidator _validator;

    public RoomBookSynchronizer(
        IUnitOfWork unitOfWork,
        ILogger<RoomBookSynchronizer> logger,
        IRoomBookSynchronizationValidator validator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _validator = validator;
    }

    public async Task SynchronizeAsync(
        Room room,
        ICollection<RoomBookDTO> dtoList,
        CancellationToken cancellationToken = default)
    {
        if (room == null)
            throw new ArgumentNullException(nameof(room));

        dtoList ??= Array.Empty<RoomBookDTO>();

        var roomBookRepo = _unitOfWork.GetRepository<RoomBook>();

        // Если коллекция не загружена, загружаем её явно
        if (room.RoomBooks == null)
        {
            var loaded = await roomBookRepo
                .GetQueryable()
                .Where(rb => rb.Room.Id.Value == room.Id.Value)
                .Include(rb => rb.Book)
                .ToListAsync(cancellationToken);
            room.RoomBooks = loaded;
        }

        var existingRoomBooks = room.RoomBooks.ToList();

        // Выполняем валидацию
        var validationResult = await _validator.ValidateAsync(room, dtoList, existingRoomBooks, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Подготовка данных
        var bookRepo = _unitOfWork.GetRepository<Book>();
        var existingDict = existingRoomBooks.ToDictionary(rb => rb.Id.Value);

        // Id записей, которые должны остаться
        var requestedIds = dtoList.Where(d => d.Id != Guid.Empty).Select(d => d.Id).ToHashSet();

        // Удаление записей, отсутствующих в запросе
        var toRemove = existingRoomBooks.Where(rb => !requestedIds.Contains(rb.Id.Value)).ToList();
        foreach (var rb in toRemove)
        {
            room.RoomBooks.Remove(rb);
            await roomBookRepo.Remove(rb, cancellationToken);
            _logger.LogDebug($"Удалена запись RoomBook с Id {rb.Id.Value} из комнаты {room.Id.Value}");
        }

        // Загрузка книг, упомянутых в DTO
        var allBookIds = dtoList.Select(d => new Id(d.BookId)).Distinct().ToList();
        var books = await bookRepo.Get(b => allBookIds.Contains(b.Id), cancellationToken);
        var bookDict = books.ToDictionary(b => b.Id.Value);

        // Проверка, что все книги найдены
        var missing = allBookIds.Select(id => id.Value).Except(bookDict.Keys).ToList();
        if (missing.Any())
            throw new LibValidationException
            {
                ExceptionDetails = missing.Select(id => $"Книга с Id {id} не найдена").ToList()
            };

        // Обработка каждого DTO
        foreach (var dto in dtoList)
        {
            var book = bookDict[dto.BookId];

            if (dto.Id != Guid.Empty && existingDict.TryGetValue(dto.Id, out var existing))
            {
                // Обновление существующей записи
                // Дублирующая проверка (для безопасности)
                if (dto.BookCount < existing.BorrowedCount)
                    throw new LibValidationException
                    {
                        ExceptionDetails = [$"Нельзя установить общее количество ({dto.BookCount}) меньше уже выданных ({existing.BorrowedCount}) для RoomBook Id {dto.Id}"]
                    };

                existing.BookCount = dto.BookCount;
                existing.Book = book; // явно обновляем связь (хотя BookId не меняется по валидатору)
                _logger.LogDebug($"Обновлена RoomBook {existing.Id.Value} с количеством {dto.BookCount}");
            }
            else
            {
                // Создание новой записи (только если dto.Id == Guid.Empty)
                // Валидатор уже проверил, что такой книги нет в комнате
                var newRoomBook = new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    Book = book,
                    Room = room,
                    BookCount = dto.BookCount,
                    BorrowedCount = 0
                };
                room.RoomBooks.Add(newRoomBook);
                _logger.LogDebug($"Создана новая RoomBook {newRoomBook.Id.Value} для книги {book.Id.Value}");
            }
        }
    }
}