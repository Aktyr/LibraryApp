namespace LibApp.Application.Commands.Settings;

public class GetPenaltySettingsCommand(IOptionsSnapshot<PenaltySettings> settings)
    : IGetQuery<EmptyRequest, PenaltySettingsResponse>, ICommand
{
    public Task<PenaltySettingsResponse> Execute(EmptyRequest request, CancellationToken cancellationToken)
    {
        var s = settings.Value;
        var dto = new PenaltySettingsDTO(
            s.DailyRate,
            s.MaxPenalty,
            s.GracePeriodDays);

        return Task.FromResult(
            new PenaltySettingsResponse("Ok", "Penalty settings retrieved successfully.", dto));
    }
}