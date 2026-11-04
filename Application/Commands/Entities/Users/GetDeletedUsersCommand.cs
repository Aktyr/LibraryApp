namespace LibApp.Application.Commands.Entities.Users;

public class GetDeletedUsersCommand(IUnitOfWork unitOfWork, IConverter<User, UserDTO> converter)
    : IGetQuery<EmptyRequest, UserResponse>, ICommand
{
    public async Task<UserResponse> Execute(EmptyRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<User>();

        // Запрос выполняется на стороне БД с игнорированием глобального фильтра
        var deletedUsers = await repo.ExecuteQueryIgnoringFiltersAsync(
            query => query.Where(u => u.IsDeleted),
            ct);

        var dtos = deletedUsers.Select(u => converter.ToDto(u)).ToArray();
        return ResponseFactory.List<User, UserDTO, UserResponse>(dtos);
    }
}