using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Mediatr.Broker;

public class Node
{
    public string Name { get; set; }
    public ConcurrentDictionary<Type?, Func<object, CancellationToken, Task<object>>> handlers { get; set; }
    public ConcurrentDictionary<Type?, Func<object, CancellationToken, Task>> notificationHandlers { get; set; }
}

public class NodeHandlerType
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public string RequestType { get; set; }
    public string ResponseType { get; set; }
    public string RequestTypeAssemblyName { get; set; }
    public string ResponseTypeAssemblyName { get; set; }
    public string ClientName { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public IPAddress IpAddress { get; set; }
}

public class NodeNotificationHandlerType
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public string NotificationType { get; set; }
    public string NotificationTypeAssemblyName { get; set; }
    public string ClientName { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public IPAddress IpAddress { get; set; }
}

public class MediatorServer : IMediatorServer
{
    private readonly int _port;
    private readonly string _host;
    private IPEndPoint _endPoint;
    private readonly string _name;
    private Socket _socket;

    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<IPEndPoint, string> _connectedClients = new();


    private ConcurrentQueue<string> _nodeQueue = new();

    private readonly Node _node = new();


    private CancellationTokenSource _cts;

    public MediatorServer(int port, string host, string name)
    {
        _port = port;
        _host = host;
        _name = name;
        _cts = new CancellationTokenSource();
        _logger = new LoggerFactory().CreateLogger<MediatorServer>();
        _endPoint = new IPEndPoint(IPAddress.Loopback, _port);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Mediator Broker is running...");

        try
        {
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(_endPoint);


            //var processMessagesTask = Task.Run(ProcessMessagesAsync, _cts.Token);


            while (true)
            {
                byte[] data = new byte[1024];

                var brokerName = Encoding.UTF8.GetBytes(_name);

                //await _udpClient.SendAsync(brokerName, brokerName.Length);

                var available = _socket.Available;

                if (_socket.Available > 0)
                {
                    Console.WriteLine("Mediator Broker is receiving...");

                    var size = await _socket.ReceiveAsync(data, cancellationToken);

                    // if (_socket.Connected == false)
                    // {
                    //     _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    //     _socket.Bind(_endPoint);
                    // }

                    if (_socket.Connected && _socket.Available > 0)
                    {
                        var payload = Encoding.UTF8.GetString(data, 0, size);

                        var nodeHandlerType = JsonSerializer.Deserialize<NodeHandlerType>(payload);

                        var nodeNotificationHandlerType = JsonSerializer.Deserialize<NodeNotificationHandlerType>(payload);

                        if (nodeHandlerType != null)
                        {
                            // add the handler to the node queue
                            _node.Name = nodeHandlerType.Name;

                            if (!_node.handlers.ContainsKey(Type.GetType(nodeHandlerType.RequestType)))
                            {
                                _node.handlers.TryAdd(Type.GetType(nodeHandlerType.RequestType), (request, cancellationToken) =>
                                    ExecuteHandlerAsync(nodeHandlerType, request, cancellationToken));
                            }

                            //_nodes.TryAdd(node.Name, node);
                            //_nodeQueue.Enqueue(node);
                            _nodeQueue.Enqueue(_node.Name);
                        }
                        else if (nodeNotificationHandlerType != null)
                        {
                            // add the notification handler to the node queue

                            _node.Name = nodeNotificationHandlerType.Name;

                            if (!_node.notificationHandlers.ContainsKey(Type.GetType(nodeNotificationHandlerType.NotificationType)))
                            {
                                _node.notificationHandlers.TryAdd(Type.GetType(nodeNotificationHandlerType.NotificationType), (notification, cancellationToken) =>
                                    ExecuteNotificationHandlerAsync(nodeNotificationHandlerType, notification, cancellationToken));
                            }

                            // _nodes.TryAdd(node.Name, node);
                            _nodeQueue.Enqueue(_node.Name);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private Task ExecuteNotificationHandlerAsync(NodeNotificationHandlerType nodeNotificationHandlerType, object notification, CancellationToken cancellationToken)
    {
        byte[] data = new byte[1024];
        // send the notification to the node
        var notificationJson = JsonSerializer.Serialize(notification);

        var notificationBytes = Encoding.UTF8.GetBytes(notificationJson);

        var endPoint = new IPEndPoint(nodeNotificationHandlerType.IpAddress, nodeNotificationHandlerType.Port);

        var result = _socket.SendToAsync(notificationBytes, SocketFlags.None, endPoint);

        return Task.CompletedTask;
    }

    private async Task<object> ExecuteHandlerAsync(NodeHandlerType nodeHandlerType, object request, CancellationToken cancellationToken)
    {
        byte[] data = new byte[1024];
        // send the request to the node
        var requestJson = JsonSerializer.Serialize(request);

        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        var endPoint = new IPEndPoint(nodeHandlerType.IpAddress, nodeHandlerType.Port);

        var result = _socket.SendToAsync(requestBytes, SocketFlags.None, endPoint);

        // wait for the response from the node
        var responseBytes = await _socket.ReceiveFromAsync(data, endPoint);

        var responseJson = Encoding.UTF8.GetString(data, 0, responseBytes.ReceivedBytes);

        var response = JsonSerializer.Deserialize(responseJson, Type.GetType(nodeHandlerType.ResponseType)!);

        return response;
    }


    private async Task ProcessMessagesAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_nodeQueue.TryDequeue(out var message))
                {
                    // Handle the message and forward it to the appropriate components
                    // For simplicity, we'll just print the message for now
                    Console.WriteLine($"Processing message: {message}");
                }

                // You can add more sophisticated processing logic here
                await Task.Delay(100); // Adjust the delay based on your needs
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in message processing: {ex.Message}");
        }
    }
}