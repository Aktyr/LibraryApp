namespace LibApp.Application.Commands.Settings;

public class GetBorrowingSettingsCommand(IOptionsSnapshot<BorrowingSettings> settings)
    : IGetQuery<EmptyRequest, BorrowingSettingsResponse>, ICommand
{
    public Task<BorrowingSettingsResponse> Execute(EmptyRequest request, CancellationToken cancellationToken)
    {
        var s = settings.Value;
        var dto = new BorrowingSettingsDTO(
            s.MaxBooksPerUser,
            s.MaxExtendDeadlineDays,
            s.MinBorrowDays,
            s.MaxBorrowDays);

        return Task.FromResult(
            new BorrowingSettingsResponse("Ok", "Borrowing settings retrieved successfully.", dto));
    }
}