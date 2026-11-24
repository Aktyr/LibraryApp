namespace LibApp.Core.DTO.Reports;

/// <summary>Строка отчёта «Текущие выдачи»: книга, которая сейчас на руках у читателя.</summary>
/// <param name="UserFullName">ФИО на текущий момент. После анонимизации читателя здесь «Deleted User».</param>
/// <param name="IsUserDeleted">Читатель деактивирован: выдача учитывается в отчёте, вход в систему закрыт.</param>
/// <param name="BorrowDate">Дата фактической выдачи в UTC.</param>
/// <param name="Deadline">Срок возврата в UTC. null означает, что срок не задан.</param>
/// <param name="DaysOverdue">Дней просрочки; 0, если срок не наступил или не задан.</param>
/// <param name="Penalty">Начисленный штраф. null: фоновый сервис ещё не начислял, отчёт штраф не вычисляет.</param>
public record CurrentIssueReportDTO(
    Guid UserRoomBookId,
    Guid UserId,
    string UserFullName,
    bool IsUserDeleted,
    string BookTitle,
    string BookAuthor,
    string RoomName,
    DateTime BorrowDate,
    DateTime? Deadline,
    int DaysOverdue,
    decimal? Penalty);