namespace LibApp.Application.Validation.Services;

/// <summary>
/// Фабрики сообщений об ошибках для RoomBook.
/// Держим в одном месте — валидатор и синхронизатор шлют одно и то же.
/// </summary>
public static class RoomBookErrors
{
    public static string BookCountLessThanBorrowedAndReserved(
        int bookCount, int borrowedCount, int reservedCount, Guid roomBookId)
            => $"Нельзя установить общее количество ({bookCount}) меньше суммы выданных ({borrowedCount}) " +
               $"и забронированных ({reservedCount}) для RoomBook Id {roomBookId}";
}