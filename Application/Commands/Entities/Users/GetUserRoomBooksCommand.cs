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

        // BorrowDTOConverter читает RoomBook.Book.Title/Author и RoomBook.Room.Name.
        // Без явных Include EF подгружает их лениво — по SQL на каждую выдачу.
        var userBooks = await userRoomBookRepo.GetAsync(
            filter: urb => urb.User.Id == userId,
            cancellationToken: cancellationToken,
            includePaths:
            [
                IncludePaths.UserRoomBook.RoomBookBook,
                IncludePaths.UserRoomBook.RoomBookRoom
            ]);

        var bookDTOs = userBooks
            .Select(urb => converter.ToDto(urb))
            .OrderBy(b => b.Deadline)
            .ToArray();

        return ResponseFactory.List<UserRoomBook, BorrowedBookDTO, BorrowResponse>(bookDTOs);
    }
}