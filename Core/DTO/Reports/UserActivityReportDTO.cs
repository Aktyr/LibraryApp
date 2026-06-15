namespace LibApp.Core.DTO.Reports;

public record UserActivityReportDTO(
    Guid UserId,
    string FullName,
    string Email,
    int TotalBorrowedCount,    // Всего брал книг
    int CurrentBorrowedCount,  // Сейчас на руках
    decimal? TotalPenalty,     // Сумма штрафов
    bool HasOverdue            // Есть ли просрочки
);
