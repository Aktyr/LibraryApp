namespace LibApp.Application.Commands.Entities.Users;

public class UpdateUserCommand(
    IUnitOfWork unitOfWork,
    UserValidatorAsync userValidator,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<UpdateUserRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = UnitOfWork.GetRepository<User>();
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

        await userRepo.UpdateAsync(user, cancellationToken);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var details = diffLines.Count > 0
            ? string.Join("\n", diffLines)
            : "Обновлён профиль (без изменений)";

        var audit = new AuditEntry(
            AuditActionType.Update,
            nameof(User),
            user.Id.Value,
            details);

        return (ResponseFactory.Updated<User>(), audit);
    }
}