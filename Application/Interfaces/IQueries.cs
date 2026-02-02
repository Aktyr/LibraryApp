namespace LibApp.Application.Interfaces;

public interface IGetQuery<TGetRequest, TGetResponse>
    where TGetRequest : IGetRequest
    where TGetResponse : IGetResponse
{
    Task<TGetResponse> Execute(TGetRequest request, CancellationToken cancellationToken);
}