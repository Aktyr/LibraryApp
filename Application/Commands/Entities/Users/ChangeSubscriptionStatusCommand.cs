namespace LibApp.Application.Commands.Entities.Users;

public class ChangeSubscriptionStatusCommand(IUnitOfWork unitOfWork)
    : ICreateOrUpdateCommand<ChangeSubscriptionStatusRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ChangeSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) throw new UserNotFoundException();

        user.SubscriptionStatus = request.NewStatus;

        await userRepo.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ResponseFactory.Success($"Статус изменён на {request.NewStatus}");
    }
}