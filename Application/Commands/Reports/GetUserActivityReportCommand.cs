namespace LibApp.Application.Commands.Reports;

public class GetUserActivityReportCommand(
    IUnitOfWork unitOfWork)
    : IGetQuery<GetUserActivityRequest, UserActivityReportResponse>, ICommand
{
    public async Task<UserActivityReportResponse> Execute(GetUserActivityRequest request, CancellationToken ct)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        // Получаем всех пользователей и все бронирования (без отслеживания внутри репозитория)
        var users = await userRepo.GetAsync();
        var borrows = await userRoomBookRepo.GetAsync();

        var query = from user in users
                    join borrow in borrows on user.Id equals borrow.User.Id into borrowsGroup
                    select new UserActivityReportDTO(
                        user.Id.Value,
                        user.FullName,
                        user.Email,
                        borrowsGroup.Count(),
                        borrowsGroup.Count(urb => !urb.IsReturned),
                        borrowsGroup.Sum(urb => urb.Penalty ?? 0),
                        borrowsGroup.Any(urb => !urb.IsReturned && urb.Deadline < DateTime.UtcNow)
                    );

        if (request.OnlyWithOverdue)
            query = query.Where(u => u.HasOverdue);
        if (request.OnlyActive)
            query = query.Where(u => u.CurrentBorrowedCount > 0);

        var result = query
            .OrderByDescending(u => u.TotalPenalty)
            .ToArray();

        return ResponseFactory.Found<User, UserActivityReportDTO, UserActivityReportResponse>(result);
    }
}