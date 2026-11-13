namespace LibApp.Core.DTO.Settings;

public record BorrowingSettingsDTO(
    int MaxBooksPerUser,
    int MaxExtendDeadlineDays,
    int MinBorrowDays,
    int MaxBorrowDays,
    int ReserveHoldHours,
    int MaxActiveRequestsPerUser);