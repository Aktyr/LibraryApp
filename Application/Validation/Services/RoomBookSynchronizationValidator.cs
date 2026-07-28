namespace LibApp.Application.Validation.Services;

public class RoomBookSynchronizationValidator : IRoomBookSynchronizationValidator, IValidator
{
    public Task<ValidationResponse> ValidateAsync(
        Room room,
        ICollection<RoomBookDTO> dtoList,
        ICollection<RoomBook> existingRoomBooks,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (room == null)
            errors.Add("Комната не может быть null");

        dtoList ??= Array.Empty<RoomBookDTO>();

        // Проверка отрицательного количества
        foreach (var dto in dtoList)
            if (dto.BookCount < 0)
                errors.Add($"Количество книг не может быть отрицательным (BookId: {dto.BookId})");

        // Проверка дубликатов BookId в запросе
        var duplicateBookIds = dtoList
            .GroupBy(d => d.BookId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        foreach (var dup in duplicateBookIds)
            errors.Add($"Книга с Id {dup} указана несколько раз в списке");

        // Для существующих записей проверяем, что BookId не меняется
        var existingDict = existingRoomBooks.ToDictionary(rb => rb.Id.Value);
        foreach (var dto in dtoList.Where(d => d.Id != Guid.Empty))
        {
            if (existingDict.TryGetValue(dto.Id, out var existing))
            {
                if (existing.Book.Id.Value != dto.BookId)
                    errors.Add($"Нельзя изменить книгу для существующей записи RoomBook (Id: {dto.Id})");

                if (dto.BookCount < existing.BorrowedCount)
                    errors.Add($"Нельзя установить общее количество ({dto.BookCount}) меньше уже выданных ({existing.BorrowedCount}) для RoomBook Id {dto.Id}");
            }
        }

        // Для новых записей (Id == Guid.Empty) запрещаем добавлять книгу, которая уже есть в комнате
        var existingBookIds = existingRoomBooks.Select(rb => rb.Book.Id.Value).ToHashSet();
        var newBookIds = dtoList
            .Where(d => d.Id == Guid.Empty)
            .Select(d => d.BookId)
            .ToHashSet();

        foreach (var bookId in newBookIds)
        {
            if (existingBookIds.Contains(bookId))
                errors.Add($"Книга с Id {bookId} уже присутствует в комнате. Используйте существующую запись (укажите Id) для изменения количества.");
        }


        return Task.FromResult(new ValidationResponse(!errors.Any(), errors));
    }
}
