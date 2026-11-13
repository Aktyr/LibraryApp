namespace LibApp.Application.Interfaces;

/// <summary>
/// Единая точка создания выдачи. Используется и в прямой выдаче библиотекарем
/// (<c>BorrowBookCommand</c>), и при одобрении заявки читателя
/// (<c>ApproveBorrowRequestCommand</c>) — чтобы проверки лимитов, просрочек и
/// инкремент <c>RoomBook.BorrowedCount</c> не разъезжались между двумя путями.
/// </summary>
public interface IBorrowingService
{
    /// <summary>
    /// Валидирует выдачу, создаёт <see cref="UserRoomBook"/>, инкрементирует
    /// <c>RoomBook.BorrowedCount</c> и возвращает созданную сущность с заполненными
    /// <c>User</c> и <c>RoomBook</c>. SaveChanges и запись аудита — на вызывающей команде.
    /// </summary>
    /// <param name="borrowRequestId">
    /// Заявка, по которой создаётся выдача. <c>null</c> — прямая выдача библиотекарем
    /// (без предварительной заявки читателя).
    /// </param>
    /// <exception cref="UserNotFoundException">Пользователь не найден.</exception>
    /// <exception cref="UserRoomBookNotFoundException">Экземпляр книги не найден (сохранено историческое имя исключения).</exception>
    /// <exception cref="LibValidationException">Нарушены бизнес-правила выдачи (лимит, просрочка, срок).</exception>
    Task<UserRoomBook> BorrowAsync(
        Id userId,
        Id roomBookId,
        int borrowDays,
        Id? borrowRequestId,
        CancellationToken cancellationToken);
}