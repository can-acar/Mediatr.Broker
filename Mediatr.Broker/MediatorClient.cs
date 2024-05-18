using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Mediatr.Broker;

public class ReceiveMessage
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required object Payload { get; set; }
}

public class MediatorClient(int port, string host, string name):IMediatorClient
{
    private readonly int _port = port;
    private readonly UdpClient _client = new();
    private ILogger<MediatorClient> _logger = new LoggerFactory().CreateLogger<MediatorClient>();
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();
    private ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _notificationHandlers = new();

    public async Task Register()
    {
        // aggregate all registered handlers and notification handlers from the mediator and send them to the mediator broker
        // _client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        await _client.Client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _port));

        // check if the client is connected to the server
        if(_client.Client.Connected)
        {
            _logger.LogInformation("Connected to the server");
            _logger.LogWarning("Connected to the server");
            Console.WriteLine("Connected to the server");
        }
        else
        {
            _logger.LogInformation("Not connected to the server");
        }

        var handlers = GetHandlers();
        var notificationHandlers = GetNotificationHandlers();
        foreach(var handler in handlers)
        {
            await SendHandler(handler);

            //var method = new  Func<object, CancellationToken, Task<object>> {(request, cancellationToken) => ExecuteHandlerAsync(handler, (IRequest<object>) request, cancellationToken)};
            _handlers.TryAdd(handler, (request, cancellationToken) => ExecuteHandlerAsync(handler, (IRequest<object>)request, cancellationToken));
        }

        foreach(var notificationHandler in notificationHandlers)
        {
            await SendNotificationHandler(notificationHandler);
        }
    }

    private Task<object> ExecuteHandlerAsync(Type handler, IRequest<object> request, CancellationToken cancellationToken)
    {
        var handlerInstance = Activator.CreateInstance(handler);
        var method = handler.GetMethod("HandleAsync");
        var result = method!.Invoke(handlerInstance, new object[] {request, cancellationToken});
        return (Task<object>)result!;
    }

    public async Task Run()
    {
        // wait for the mediator broker to send a message back and then handle it accordingly 
        while(!_cts.Token.IsCancellationRequested)
        {
            var result = await _client.ReceiveAsync(cancellationToken: _cts.Token);
            var message = Encoding.UTF8.GetString(result.Buffer);
            var data = JsonSerializer.Deserialize<ReceiveMessage>(message);
            Console.WriteLine($"Receive message: {data!.Name}");
            var handlerType = typeof(IHandler<,>);
            var notificationHandlerType = typeof(INotificationHandler<>);

            // cast the data payload to a NodeHandlerType or NodeNotificationHandlerType
            if(data.Type == "Handler")
            {
                var item = JsonSerializer.Deserialize<NodeHandlerType>(JsonSerializer.Serialize(data.Payload));
                var requestType = Assembly.Load(item.RequestTypeAssemblyName).GetType(item.RequestType);
                var responseType = Assembly.Load(item.ResponseTypeAssemblyName).GetType(item.ResponseType);
                var handler = typeof(MediatorClient).GetMethod("Handler")!.MakeGenericMethod(requestType, responseType);
                await (Task)handler.Invoke(this, new object[] {data.Payload});
            }
            else if(data.Type == "NotificationHandler")
            {
                var item = JsonSerializer.Deserialize<NodeNotificationHandlerType>(JsonSerializer.Serialize(data.Payload));
                var notificationType = Assembly.Load(item.NotificationTypeAssemblyName).GetType(item.NotificationType);
                var handler = typeof(MediatorClient).GetMethod("Notify")!.MakeGenericMethod(notificationType);
                await (Task)handler.Invoke(this, new object[] {data.Payload});
            }

            // deserialize the message into a payload

            // get the message and handle it accordingly
        }
    }

    private IEnumerable<Type> GetHandlers()
    {
        var handlerType = typeof(IHandler<,>);
        var handlerTypes = Assembly.GetEntryAssembly()!.GetTypes()
                                   .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType));
        return handlerTypes;
    }

    private IEnumerable<Type> GetNotificationHandlers()
    {
        var notificationHandlerType = typeof(INotificationHandler<>);
        var notificationHandlerTypes = Assembly.GetEntryAssembly()!.GetTypes()
                                               .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == notificationHandlerType));
        return notificationHandlerTypes;
    }

    private async Task SendHandler(Type handler)
    {
        var handlerName = handler.Name;
        var requestType = handler.GetInterfaces().First().GetGenericArguments().First();
        var responseType = handler.GetInterfaces().First().GetGenericArguments().Last();
        var requestTypeName = requestType.Name;
        var responseTypeName = responseType.Name;
        var requestTypeAssemblyName = requestType.Assembly.GetName().Name;
        var responseTypeAssemblyName = responseType.Assembly.GetName().Name;
        var message = new
        {
            Id = Guid.NewGuid(),
            Type = "Handler",
            Name = handlerName,
            Token = "",
            RequestType = requestTypeName,
            ResponseType = responseTypeName,
            RequestTypeAssemblyName = requestTypeAssemblyName,
            ResponseTypeAssemblyName = responseTypeAssemblyName,
            Host = host,
            Port = _port,
            ClientName = name,
            IpAddress = Dns.GetHostAddresses(Dns.GetHostName()).First()
        };
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _client.SendAsync(bytes, bytes.Length, host, _port);
    }

    private async Task SendNotificationHandler(Type notificationHandler)
    {
        var handlerName = notificationHandler.Name;
        var notificationType = notificationHandler.GetInterfaces().First().GetGenericArguments().First();
        var notificationTypeName = notificationType.Name;
        var notificationTypeAssemblyName = notificationType.Assembly.GetName().Name;
        var message = new
        {
            Id = Guid.NewGuid(),
            Type = "NotificationHandler",
            Name = handlerName,
            NotificationType = notificationTypeName,
            NotificationTypeAssemblyName = notificationTypeAssemblyName,
            Host = host,
            Port = _port,
            ClientName = name,
            IpAddress = Dns.GetHostAddresses(Dns.GetHostName()).First()
        };
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _client.SendAsync(bytes, bytes.Length, host, _port);
    }
}

public interface IMediatorClient
{
    Task Register();
    Task Run();
}