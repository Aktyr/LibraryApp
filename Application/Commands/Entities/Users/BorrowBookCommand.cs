namespace LibApp.Application.Commands.Entities.Users;

public class BorrowBookCommand : ICreateOrUpdateCommand<BorrowBookRequest, BasicCreateDeleteResponse>, ICommand
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;
    private readonly BorrowingValidatorAsync _validator;

    public BorrowBookCommand(
        IRepository<User> userRepo,
        IRepository<RoomBook> roomBookRepo,
        BorrowingValidatorAsync validator)
    {
        _userRepo = userRepo;
        _roomBookRepo = roomBookRepo;
        _validator = validator;
    }

    public async Task<BasicCreateDeleteResponse> Execute(BorrowBookRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var user = (await _userRepo.Get(u => u.Id.Value == request.UserId, cancellationToken)).FirstOrDefault()
            ?? throw new UserNotFoundException();

        var roomBook = (await _roomBookRepo.Get(rb => rb.Id.Value == request.RoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new UserRoomBookNotFoundException(); 

        var validationResult = await _validator.ValidateBorrowAsync(user, roomBook, request.BorrowDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Создаем запись о выдаче
        var userRoomBook = new UserRoomBook
        {
            User = user,
            RoomBook = roomBook,
            BorrowDate = DateTime.Now,
            Deadline = DateTime.Now.AddDays(request.BorrowDays)
        };

        // Обновляем счетчик
        roomBook.BorrowedCount++;
        user.RoomBooks.Add(userRoomBook);

        // Сохраняем
        await _roomBookRepo.Update(roomBook, cancellationToken);
        await _userRepo.Update(user, cancellationToken);

        return ResponseFactory.Success($"Книга выдана. Срок возврата: {userRoomBook.Deadline:d}");
    }
}