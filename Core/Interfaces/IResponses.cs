namespace LibApp.Core.Interfaces;

public interface IResponse
{
    string Status { get; }
    string Message { get; }
}
public interface IAddOrUpdateResponse : IResponse;
public interface IDeleteResponse : IResponse;
public interface IGetResponse : IResponse;
