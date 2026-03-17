using LibApp.Core.DTO.Borrowing;

namespace LibApp.Application.Commands.Borrowing;

public class GetUserBooksQuery : IGetQuery<GetUserBooksRequest, UserBooksResponse>
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly IConverter<UserRoomBook, BorrowedBookDTO> _converter;

    public GetUserBooksQuery(
        IRepository<UserRoomBook> userRoomBookRepo,
        IConverter<UserRoomBook, BorrowedBookDTO> converter)
    {
        _userRoomBookRepo = userRoomBookRepo;
        _converter = converter;
    }

    public async Task<UserBooksResponse> Execute(GetUserBooksRequest request, CancellationToken cancellationToken)
    {
        var userBooks = await _userRoomBookRepo.Get(
            urb => urb.User.Id.Value == request.UserId,
            cancellationToken);

        var bookDTOs = userBooks
            .Select(urb => _converter.ToDto(urb))
            .OrderBy(b => b.Deadline)
            .ToArray();

        return new UserBooksResponse("Ok", "Список книг получен", bookDTOs);
    }
}