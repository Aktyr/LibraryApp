namespace LibApp.Application.Validation.Services;

/// <summary>
/// Фабрики сообщений об ошибках для RoomBook.
/// Держим в одном месте — валидатор и синхронизатор шлют одно и то же.
/// </summary>
public static class RoomBookErrors
{
    public static string BookCountLessThanBorrowed(int bookCount, int borrowedCount, Guid roomBookId)
        => $"Нельзя установить общее количество ({bookCount}) меньше уже выданных ({borrowedCount}) для RoomBook Id {roomBookId}";
}