namespace Mediatr.Broker;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Mediator : IMediator
{
    private readonly ILogger<Mediator> _logger;

    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers =
        new ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>>();

    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _notificationHandlers =
        new ConcurrentDictionary<Type, Func<object, CancellationToken, Task>>();

    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = new LoggerFactory().CreateLogger<Mediator>();
    }


    public void RegisterNotificationHandler<TNotification>(Func<TNotification, CancellationToken, Task> handler) where TNotification : INotification
    {
        if (handler == null)
        {
            _logger.LogError("Handler cannot be null");
            throw new ArgumentNullException(nameof(handler), "Handler cannot be null.");
        }

        var notificationType = typeof(TNotification);

        if (!_notificationHandlers.TryGetValue(notificationType, out var handlers))
        {
            handlers = (notification, cancellationToken) => handler((TNotification) notification, cancellationToken);

            _notificationHandlers.TryAdd(notificationType, handlers);
        }

        _logger.LogInformation("Registered notification handler for {NotificationTypeFullName}", notificationType.FullName);
    }


    public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null.");
        }

        var requestType = typeof(TRequest);

        if (_handlers.TryGetValue(requestType, out var handlers))
        {
            var handlerTasks = handlers(request, cancellationToken).ContinueWith(task => (TResponse) task.Result!, cancellationToken);

            return Task.WhenAll(handlerTasks).ContinueWith(task => (TResponse) task.Result.First(), cancellationToken);
        }

        throw new InvalidOperationException($"No handler registered for request type {requestType.FullName}");
    }

    public async Task Notify<TNotification>()
        where TNotification : INotification

    {
        var notificationType = typeof(TNotification);

        if (_notificationHandlers.TryGetValue(notificationType, out var handlers))
        {
            await handlers(Activator.CreateInstance(notificationType)!, CancellationToken.None);
        }
        else
        {
            throw new InvalidOperationException($"No handler registered for notification type {notificationType.FullName}");
        }
    }
}

public static class MediatorExtension
{
    public static void RegisterHandler<TRequest, THandler>(this IServiceCollection services)
        where TRequest : class, IRequest
        where THandler : class, IHandler<TRequest, Guid>
    {
        services.AddScoped<IHandler<TRequest, Guid>, THandler>();
    }
    
    
    public static void RegisterHandlers<TRequest, THandler>(this IServiceCollection services)
        where TRequest : class, IRequest 
        where THandler : class, IHandler<TRequest, Guid>
    {
        var handlerType = typeof(THandler);
        var requestType = typeof(TRequest);

        var handlerInterface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<,>));

        if (handlerInterface == null)
        {
            throw new InvalidOperationException($"Handler {handlerType.FullName} does not implement IHandler<,>.");
        }

        var handlerRequestType = handlerInterface.GetGenericArguments()[0];

        if (handlerRequestType != requestType)
        {
            throw new InvalidOperationException(
                $"Handler {handlerType.FullName} does not implement IHandler<{requestType.FullName},>.");
        }

        services.AddScoped(handlerInterface, handlerType);
    }
}