namespace LibApp.Application.Commands.Entities.Users;

public class ExtendDeadlineCommand : ICreateOrUpdateCommand<ExtendDeadlineRequest, BasicCreateDeleteResponse>, ICommand
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly BorrowingValidatorAsync _validator;

    public ExtendDeadlineCommand(IRepository<UserRoomBook> userRoomBookRepo, BorrowingValidatorAsync validator)
    {
        _userRoomBookRepo = userRoomBookRepo;
        _validator = validator;
    }

    public async Task<BasicCreateDeleteResponse> Execute(ExtendDeadlineRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var userRoomBook = (await _userRoomBookRepo.Get(urb => urb.Id.Value == request.UserRoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new UserRoomBookNotFoundException();

        var validationResult = await _validator.ValidateExtendAsync(userRoomBook, request.ExtraDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Продлеваем срок
        userRoomBook.Deadline = userRoomBook.Deadline!.Value.AddDays(request.ExtraDays);

        // Сохраняем
        await _userRoomBookRepo.Update(userRoomBook, cancellationToken);

        return ResponseFactory.Success($"Срок продлен до {userRoomBook.Deadline:d}");
    }
}