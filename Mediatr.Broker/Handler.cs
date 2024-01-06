namespace Mediatr.Broker;

public abstract class Handler<TRequest, TResponse> : IHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private IHandler<TRequest, TResponse>? _handlerImplementation;

    Task<TResponse> IHandler<TRequest, TResponse>.Handle(TRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Handle(request));
    }


    protected abstract TResponse Handle(TRequest request);
}

public abstract class Handler<TRequest> : IHandler<TRequest, Guid>
    where TRequest : IRequest<Guid>
{
    public Task<Guid> Handle(TRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Handle(request));
    }

    protected abstract Guid Handle(TRequest request);
}