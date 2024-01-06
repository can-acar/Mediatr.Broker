namespace Mediatr.Broker;

public interface IMediator
{
    // void RegisterHandler<THandler, TRequest>()
    //     where TRequest : IRequest, IRequest<Guid>
    //     where THandler : IHandler<TRequest>;

    void RegisterNotificationHandler<TNotification>(Func<TNotification, CancellationToken, Task> handler) where TNotification : INotification;

    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;

    Task Notify<TNotification>() where TNotification : INotification;
}