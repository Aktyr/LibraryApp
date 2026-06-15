namespace LibApp.Core.DTO.Reports;

public record BookPopularityReportDTO(
    Guid BookId,
    string Title,
    string Author,
    int TotalBorrowedCount,
    int CurrentBorrowedCount
);
