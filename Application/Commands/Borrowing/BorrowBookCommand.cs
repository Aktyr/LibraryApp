namespace LibApp.Application.Commands.Borrowing;
public class BorrowBookCommand : ICreateOrUpdateCommand<BorrowBookRequest, BasicCreateDeleteResponse>
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly BorrowingValidatorAsync _validator;

    public BorrowBookCommand(
        IRepository<User> userRepo,
        IRepository<RoomBook> roomBookRepo,
        IRepository<UserRoomBook> userRoomBookRepo,
        BorrowingValidatorAsync validator)
    {
        _userRepo = userRepo;
        _roomBookRepo = roomBookRepo;
        _userRoomBookRepo = userRoomBookRepo;
        _validator = validator;
    }

    public async Task<BasicCreateDeleteResponse> Execute(BorrowBookRequest request, CancellationToken cancellationToken)
    {
        // 1. Получаем сущности
        var user = (await _userRepo.Get(u => u.Id.Value == request.UserId, cancellationToken)).FirstOrDefault()
            ?? throw new UserNotFoundException();

        var roomBook = (await _roomBookRepo.Get(rb => rb.Id.Value == request.RoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new Exception("Книга не найдена"); // TODO: Создать RoomBookNotFoundException

        // 2. Валидация бизнес-правил
        var validationResult = await _validator.ValidateBorrowAsync(user, roomBook, request.BorrowDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // 3. Создаем запись о выдаче
        var userRoomBook = new UserRoomBook
        {
            User = user,
            RoomBook = roomBook,
            BorrowDate = DateTime.Now,
            Deadline = DateTime.Now.AddDays(request.BorrowDays)
        };

        // 4. Обновляем счетчик
        roomBook.BorrowedCount++;

        // 5. Сохраняем
        await _userRoomBookRepo.Add(userRoomBook, cancellationToken);
        await _roomBookRepo.Update(roomBook, cancellationToken);

        return new BasicCreateDeleteResponse("Ok", $"Книга выдана. Срок возврата: {userRoomBook.Deadline:d}");
    }
}