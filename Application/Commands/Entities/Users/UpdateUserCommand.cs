namespace LibApp.Application.Commands.Entities.Users;

public class UpdateUserCommand(
    IUnitOfWork unitOfWork,
    UserValidatorAsync userValidator,
    IAuditService auditService) : ICreateOrUpdateCommand<UpdateUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        var diffLines = new List<string>();
        void AddDiff(string field, object? oldVal, object? newVal)
        {
            var diff = AuditDiffBuilder.FormatChange(field, oldVal, newVal);
            if (!string.IsNullOrEmpty(diff)) diffLines.Add(diff);
        }

        AddDiff("Изменена фамилия", user.LastName, request.LastName);
        AddDiff("Изменено имя", user.FirstName, request.FirstName);
        AddDiff("Изменено отчество", user.MiddleName, request.MiddleName);
        AddDiff("Изменена контактная информация", user.ContactInfo, request.ContactInfo);

        // Валидация
        var validationResult = await userValidator.ValidateNameAndContactAsync(
            request.LastName,
            request.FirstName,
            request.MiddleName,
            request.ContactInfo);

        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Обновляем только если валидация прошла
        user.LastName = request.LastName;
        user.FirstName = request.FirstName;
        user.MiddleName = request.MiddleName;
        user.ContactInfo = request.ContactInfo;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await userRepo.UpdateAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var details = diffLines.Count > 0
                ? string.Join("\n", diffLines)
                : "Обновлён профиль (без изменений)";

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(User),
                user.Id.Value,
                details: details,
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Updated<User>();
    }
}