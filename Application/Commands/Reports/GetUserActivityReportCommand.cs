namespace LibApp.Application.Commands.Reports;

public class GetUserActivityReportCommand(
    IUnitOfWork unitOfWork)
    : IGetQuery<GetUserActivityRequest, UserActivityReportResponse>, ICommand
{
    public async Task<UserActivityReportResponse> Execute(GetUserActivityRequest request, CancellationToken ct)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var usersQuery = userRepo.GetQueryable();
        var borrowsQuery = userRoomBookRepo.GetQueryable();

        var query = from user in usersQuery
                    join borrow in borrowsQuery on user.Id equals borrow.User.Id into borrowsGroup
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

        var result = await query
            .OrderByDescending(u => u.TotalPenalty)
            .ToListAsync(ct);

        return ResponseFactory.Found<User, UserActivityReportDTO, UserActivityReportResponse>(result.ToArray());
    }
}