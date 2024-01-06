namespace Mediatr.Broker;

public interface IMediatorServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
}