namespace LibApp.Application.Commands.Entities.Users;

public class BorrowBookCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator) : ICreateOrUpdateCommand<BorrowBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(BorrowBookRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        // Валидация
        var userId = new Id(request.UserId);
        var roomBookId = new Id(request.RoomBookId);

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Id == roomBookId, cancellationToken);
        if (roomBook == null) throw new UserRoomBookNotFoundException();

        if (await userRoomBookRepo.AnyAsync(urb => urb.User.Id == userId
                                                && urb.RoomBook.Id == roomBookId
                                                && urb.ReturnDate == null, cancellationToken))
            throw new LibValidationException { ExceptionDetails = ["Пользователь уже взял эту книгу и ещё не вернул её"] };

        var validationResult = await validator.ValidateBorrowAsync(user, roomBook, request.BorrowDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            RoomBook = roomBook,
            BorrowDate = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(request.BorrowDays)
        };

        roomBook.BorrowedCount++;
        user.RoomBooks.Add(userRoomBook);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ResponseFactory.Success($"Книга выдана. Срок возврата: {userRoomBook.Deadline:d}");
    }
}