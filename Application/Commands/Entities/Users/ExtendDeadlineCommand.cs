namespace LibApp.Application.Commands.Entities.Users;

public class ExtendDeadlineCommand(
    IRepository<UserRoomBook> userRoomBookRepo,
    BorrowingValidatorAsync validator) : ICreateOrUpdateCommand<ExtendDeadlineRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ExtendDeadlineRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var userRoomBook = (await userRoomBookRepo.Get(urb => urb.Id.Value == request.UserRoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new UserRoomBookNotFoundException();

        var validationResult = await validator.ValidateExtendAsync(userRoomBook, request.ExtraDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Продлеваем срок
        userRoomBook.Deadline = userRoomBook.Deadline!.Value.AddDays(request.ExtraDays);

        // Сохраняем
        await userRoomBookRepo.Update(userRoomBook, cancellationToken);

        return ResponseFactory.Success($"Срок продлен до {userRoomBook.Deadline:d}");
    }
}