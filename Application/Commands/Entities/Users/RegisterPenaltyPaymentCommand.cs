namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Принимает оплату штрафа. Частичные оплаты не поддерживаются: сумма всегда равна
/// полной сумме штрафа, на одну выдачу — не более одного платежа (UNIQUE-индекс).
/// </summary>
public class RegisterPenaltyPaymentCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<RegisterPenaltyPaymentRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        RegisterPenaltyPaymentRequest request, CancellationToken ct)
    {
        var urbRepo = UnitOfWork.GetRepository<UserRoomBook>();
        var paymentRepo = UnitOfWork.GetRepository<PenaltyPayment>();

        var urbId = new Id(request.UserRoomBookId);

        // Eager-load: PenaltyPayments — для проверки «уже оплачен»,
        // RoomBook.Book и User — для текста аудита.
        var urb = (await urbRepo.GetAsync(
            filter: u => u.Id == urbId,
            cancellationToken: ct,
            includePaths:
            [
                IncludePaths.UserRoomBook.PenaltyPayments,
                IncludePaths.UserRoomBook.RoomBookBook,
                IncludePaths.UserRoomBook.User
            ])).FirstOrDefault();

        if (urb == null) throw new UserRoomBookNotFoundException();

        var errors = PenaltyPaymentPolicy.Validate(
            urb.Penalty,
            urb.ReturnDate,
            hasExistingPayment: urb.PenaltyPayments.Count > 0);

        if (errors.Count > 0)
            throw new LibValidationException { ExceptionDetails = errors };

        var cashierId = currentUser.LibraryUserId
            ?? throw new UnauthorizedException("Не удалось определить кассира");

        var payment = new PenaltyPayment
        {
            Id = new Id(Guid.NewGuid()),
            UserRoomBookId = urb.Id,
            Amount = urb.Penalty!.Value,
            PaidAt = DateTime.UtcNow,
            PaidByUserId = cashierId,
            Notes = request.Notes
        };
        urb.PenaltyPayments.Add(payment);

        await paymentRepo.AddAsync(payment, ct);

        var audit = new AuditEntry(
            AuditActionType.PayPenalty,
            nameof(PenaltyPayment),
            payment.Id.Value,
            $"Принята оплата штрафа {payment.Amount:0.##} ₽ за книгу «{urb.RoomBook.Book.Title}». Читатель: {urb.User.FullName}");

        return (ResponseFactory.Success($"Оплата {payment.Amount:0.##} ₽ принята"), audit);
    }
}