namespace Mediatr.Broker;

public interface IRequest : IRequest<Guid>;

public interface IRequest<out TResponse> : IBaseRequest;

public interface IBaseRequest;