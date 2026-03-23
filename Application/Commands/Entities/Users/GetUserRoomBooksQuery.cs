namespace LibApp.Application.Commands.Entities.Users;

public class GetUserRoomBooksQuery : IGetQuery<GetUserBooksRequest, BorrowResponse>
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly IConverter<UserRoomBook, BorrowedBookDTO> _converter;
    private readonly BorrowingValidatorAsync _validator;

    public GetUserRoomBooksQuery(IRepository<UserRoomBook> userRoomBookRepo, IConverter<UserRoomBook, BorrowedBookDTO> converter, BorrowingValidatorAsync validator)
    {
        _userRoomBookRepo = userRoomBookRepo;
        _converter = converter;
        _validator = validator;
    }

    public async Task<BorrowResponse> Execute(GetUserBooksRequest request, CancellationToken cancellationToken)
    {
        // Получение списка книг
        var userBooks = await _userRoomBookRepo.Get(
            urb => urb.User.Id.Value == request.UserId,
            cancellationToken);

        var bookDTOs = userBooks
            .Select(urb => _converter.ToDto(urb))
            .OrderBy(b => b.Deadline)
            .ToArray();

        return new BorrowResponse("Ok", "Список книг получен", bookDTOs);
    }
}