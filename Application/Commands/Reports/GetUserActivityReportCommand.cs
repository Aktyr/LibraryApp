namespace LibApp.Application.Commands.Reports;

public class GetUserActivityReportCommand : IGetQuery<GetUserActivityRequest, UserActivityReportResponse>, ICommand
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;

    public GetUserActivityReportCommand(
        IRepository<User> userRepo,
        IRepository<UserRoomBook> userRoomBookRepo)
    {
        _userRepo = userRepo;
        _userRoomBookRepo = userRoomBookRepo;
    }

    public async Task<UserActivityReportResponse> Execute(GetUserActivityRequest request, CancellationToken ct)
    {
        var users = await _userRepo.Get(ct);
        var allBorrows = await _userRoomBookRepo.Get(ct);

        var result = users.Select(user => new UserActivityReportDTO(
            UserId: user.Id.Value,
            FullName: user.FullName,
            Email: user.Email,
            TotalBorrowedCount: allBorrows.Count(urb => urb.User.Id.Value == user.Id.Value),
            CurrentBorrowedCount: allBorrows.Count(urb => urb.User.Id.Value == user.Id.Value && !urb.IsReturned),
            TotalPenalty: allBorrows.Where(urb => urb.User.Id.Value == user.Id.Value).Sum(urb => urb.Penalty ?? 0),
            HasOverdue: allBorrows.Any(urb => urb.User.Id.Value == user.Id.Value && !urb.IsReturned && urb.Deadline < DateTime.Now)
        )).ToArray();

        if (request.OnlyWithOverdue)
            result = result.Where(u => u.HasOverdue).ToArray();

        if (request.OnlyActive)
            result = result.Where(u => u.CurrentBorrowedCount > 0).ToArray();

        result = result.OrderByDescending(u => u.TotalPenalty).ToArray();

        return new UserActivityReportResponse("Ok", $"Найдено пользователей: {result.Length}", result);
    }
}
