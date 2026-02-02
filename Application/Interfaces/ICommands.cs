namespace LibApp.Application.Interfaces;

public interface ICreateOrUpdateCommand<TAddOrUpdateRequest, TAddOrUpdateResponse>
    where TAddOrUpdateRequest : IAddOrUpdateRequest
    where TAddOrUpdateResponse : IAddOrUpdateResponse
{
    Task<TAddOrUpdateResponse> Execute(TAddOrUpdateRequest request, CancellationToken cancellationToken);
}

public interface IDeleteCommand<TDeleteRequest, TDeleteResponse>
    where TDeleteRequest : IDeleteRequest
    where TDeleteResponse : IDeleteResponse
{
    Task<TDeleteResponse> Execute(TDeleteRequest request, CancellationToken cancellationToken);
}