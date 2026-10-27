namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Settings")]
[Route("/api/settings")]
[AllowAnonymous]
public class SettingsEndpoints : EndpointBase
{
    [HttpGet("/borrowing")]
    public async Task<IResult> GetBorrowingSettings(GetBorrowingSettingsCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new EmptyRequest(), ct));

    [HttpGet("/penalty")]
    public async Task<IResult> GetPenaltySettings(GetPenaltySettingsCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new EmptyRequest(), ct));
}