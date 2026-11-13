namespace LibApp.Application.Commands.Entities.Users;

public class BorrowBookCommand(
    IUnitOfWork unitOfWork,
    IBorrowingService borrowingService,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<BorrowBookRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        BorrowBookRequest request, CancellationToken cancellationToken)
    {
        var userRoomBook = await borrowingService.BorrowAsync(
            userId: new Id(request.UserId),
            roomBookId: new Id(request.RoomBookId),
            borrowDays: request.BorrowDays,
            borrowRequestId: null,
            cancellationToken);

        var audit = new AuditEntry(
            AuditActionType.Borrow,
            nameof(UserRoomBook),
            userRoomBook.Id.Value,
            $"Выдана книга «{userRoomBook.RoomBook.Book.Title}» пользователю «{userRoomBook.User.FullName}». Срок: {userRoomBook.Deadline:d}");

        return (ResponseFactory.Success($"Книга выдана. Срок возврата: {userRoomBook.Deadline:d}"), audit);
    }
}