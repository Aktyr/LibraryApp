namespace LibApp.Application.Commands.Entities.Users;

public class ExtendDeadlineCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator) : ICreateOrUpdateCommand<ExtendDeadlineRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ExtendDeadlineRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        // Валидация
        var userRoomBookId = new Id(request.UserRoomBookId);
        var userRoomBook = await userRoomBookRepo.FirstOrDefaultAsync(urb => urb.Id == userRoomBookId, cancellationToken);
        if (userRoomBook == null) throw new UserRoomBookNotFoundException();

        var validationResult = await validator.ValidateExtendAsync(userRoomBook, request.ExtraDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Продлеваем срок
        userRoomBook.Deadline = userRoomBook.Deadline!.Value.AddDays(request.ExtraDays);
        userRoomBook.TotalExtendedDays += request.ExtraDays;

        // Сохраняем
        await userRoomBookRepo.UpdateAsync(userRoomBook, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Success($"Срок продлен до {userRoomBook.Deadline:d}");
    }
}