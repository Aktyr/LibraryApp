namespace LibApp.Core.Configuration;

[MinMaxValidator(nameof(MinBorrowDays), nameof(MaxBorrowDays))]
public class BorrowingSettings : ISettings
{
    [PositiveNumber] [MaxRange(10)]
    public int MaxBooksPerUser { get; set; } = 5;


    [PositiveNumber] [MaxRange(14)]
    public int MaxExtendDeadlineDays { get; set; } = 14;


    [PositiveNumber]
    public int MinBorrowDays { get; set; } = 1;


    [PositiveNumber]
    public int MaxBorrowDays { get; set; } = 30;
}
