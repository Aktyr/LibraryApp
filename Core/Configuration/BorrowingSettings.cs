namespace LibApp.Core.Configuration;

[MinMaxValidator(nameof(MinBorrowDays), nameof(MaxBorrowDays))]
public class BorrowingSettings : ISettings
{
    [Range(1, 10, ErrorMessage = "MaxBooksPerUser must be between 1 and 10")]
    public int MaxBooksPerUser { get; set; } = 5;


    [Range(1, 7, ErrorMessage = "MinBorrowDays must be between 1 and 7")]
    public int MinBorrowDays { get; set; } = 1;


    [Range(7, 30, ErrorMessage = "MinBorrowDays must be between 7 and 30")]
    public int MaxBorrowDays { get; set; } = 30;


    [Range(0, 14, ErrorMessage = "MinBorrowDays must be between 0 and 14")]
    public int MaxExtendDeadlineDays { get; set; } = 14;
}
