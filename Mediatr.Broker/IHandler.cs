namespace Mediatr.Broker;

public interface IHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>

{

    
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);

   
}

public interface IHandler<in TRequest> : IHandler<TRequest, Guid>
    where TRequest : IRequest<Guid>
{
}