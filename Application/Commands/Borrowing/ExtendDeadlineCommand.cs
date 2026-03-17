namespace LibApp.Application.Commands.Borrowing;

public class ExtendDeadlineCommand : ICreateOrUpdateCommand<ExtendDeadlineRequest, BasicCreateDeleteResponse>
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly BorrowingValidatorAsync _validator;

    public ExtendDeadlineCommand(
        IRepository<UserRoomBook> userRoomBookRepo,
        BorrowingValidatorAsync validator)
    {
        _userRoomBookRepo = userRoomBookRepo;
        _validator = validator;
    }

    public async Task<BasicCreateDeleteResponse> Execute(ExtendDeadlineRequest request, CancellationToken cancellationToken)
    {
        // 1. Получаем запись о выдаче
        var userRoomBook = (await _userRoomBookRepo.Get(urb => urb.Id.Value == request.UserRoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new Exception("Запись о выдаче не найдена");

        // 2. Валидация
        var validationResult = await _validator.ValidateExtendAsync(userRoomBook, request.ExtraDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // 3. Продлеваем срок
        userRoomBook.Deadline = userRoomBook.Deadline.Value.AddDays(request.ExtraDays);

        // 4. Сохраняем
        await _userRoomBookRepo.Update(userRoomBook, cancellationToken);

        return new BasicCreateDeleteResponse("Ok", $"Срок продлен до {userRoomBook.Deadline:d}");
    }
}