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
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id.Value == request.UserId, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Id.Value == request.RoomBookId, cancellationToken);
        if (roomBook == null) throw new UserRoomBookNotFoundException();

        if (await userRoomBookRepo.AnyAsync(urb => urb.User.Id.Value == request.UserId 
                                                && urb.RoomBook.Id.Value == request.RoomBookId 
                                                && !urb.IsReturned, cancellationToken))
            throw new LibValidationException { ExceptionDetails = ["Пользователь уже взял эту книгу и ещё не вернул её"] };

        var validationResult = await validator.ValidateBorrowAsync(user, roomBook, request.BorrowDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        var userRoomBook = new UserRoomBook
        {
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