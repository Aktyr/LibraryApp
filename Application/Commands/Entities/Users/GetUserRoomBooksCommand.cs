namespace LibApp.Application.Commands.Entities.Users;

public class GetUserRoomBooksCommand(
    IUnitOfWork unitOfWork,
    IConverter<UserRoomBook, BorrowedBookDTO> converter)
    : IGetQuery<GetUserBooksRequest, BorrowResponse>, ICommand
{
    public async Task<BorrowResponse> Execute(GetUserBooksRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var userId = new Id(request.UserId);

        // PenaltyPayments — для вычисляемого IsPenaltyPaid в конвертере.
        // Без eager-load lazy-прокси даст по SQL на каждую выдачу.
        var userBooks = await userRoomBookRepo.GetAsync(
            filter: urb => urb.User.Id == userId,
            cancellationToken: cancellationToken,
            includePaths:
            [
                IncludePaths.UserRoomBook.RoomBookBook,
                IncludePaths.UserRoomBook.RoomBookRoom,
                IncludePaths.UserRoomBook.PenaltyPayments
            ]);

        var bookDTOs = userBooks
            .Select(urb => converter.ToDto(urb))
            .OrderBy(b => b.Deadline)
            .ToArray();

        return ResponseFactory.List<UserRoomBook, BorrowedBookDTO, BorrowResponse>(bookDTOs);
    }
}