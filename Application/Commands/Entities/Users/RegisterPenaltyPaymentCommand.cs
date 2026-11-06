namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Принимает оплату штрафа. Частичные оплаты не поддерживаются: сумма всегда равна
/// полной сумме штрафа, на одну выдачу — не более одного платежа (UNIQUE-индекс).
/// </summary>
public class RegisterPenaltyPaymentCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : ICreateOrUpdateCommand<RegisterPenaltyPaymentRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(RegisterPenaltyPaymentRequest request, CancellationToken ct)
    {
        var urbRepo = unitOfWork.GetRepository<UserRoomBook>();
        var paymentRepo = unitOfWork.GetRepository<PenaltyPayment>();

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

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await paymentRepo.AddAsync(payment, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(PenaltyPayment),
                payment.Id.Value,
                details: $"Принята оплата штрафа {payment.Amount:0.##} ₽ за книгу «{urb.RoomBook.Book.Title}». Читатель: {urb.User.FullName}",
                ct: ct);
        }, ct);

        return ResponseFactory.Success($"Оплата {payment.Amount:0.##} ₽ принята");
    }
}