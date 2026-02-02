namespace LibApp.Core.Interfaces;

public interface IResponse
{
    string Status { get; }
}
public interface IAddOrUpdateResponse : IResponse;
public interface IDeleteResponse : IResponse;
public interface IGetResponse : IResponse;
