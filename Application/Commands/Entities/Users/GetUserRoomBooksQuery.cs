namespace LibApp.Application.Commands.Entities.Users;

public class GetUserRoomBooksQuery(
    IUnitOfWork unitOfWork,
    IConverter<UserRoomBook, BorrowedBookDTO> converter,
    BorrowingValidatorAsync validator) : IGetQuery<GetUserBooksRequest, BorrowResponse>, ICommand
{
    public async Task<BorrowResponse> Execute(GetUserBooksRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        // Получение списка книг
        var userBooks = await userRoomBookRepo.GetAsync(
            urb => urb.User.Id.Value == request.UserId,
            cancellationToken);

        var bookDTOs = userBooks
            .Select(urb => converter.ToDto(urb))
            .OrderBy(b => b.Deadline)
            .ToArray();

        return ResponseFactory.List<UserRoomBook, BorrowedBookDTO, BorrowResponse>(bookDTOs);
    }
}