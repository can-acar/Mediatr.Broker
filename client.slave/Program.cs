// See https://aka.ms/new-console-template for more information

using client.slave;
using Mediatr.Broker;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = new ServiceCollection()
    .AddSingleton<IMediator, Mediator>()
    .AddSingleton<IMediatorClient>(new MediatorClient(3333, "127.0.0.1", "client"));

serviceProvider.RegisterHandler<SampleRequest, SampleHandler>();


// add IHandler, IRequest, IRequest<,>  to the DI container
// builder.Services.AddSingleton<IMediator, Mediator>();
//
// builder.Services.AddSingleton<IMediatorClient>(new MediatorClient(3333, "127.0.0.1", "client"));
//
// builder.Services.RegisterHandler<SampleRequest,SampleHandler>();

// mediator.RegisterHandler<SampleHandler, SampleRequest>();

var app = serviceProvider.BuildServiceProvider();

var client = app.GetService<IMediatorClient>();


client?.Register();

client?.Run();


while (true)
{
    Console.WriteLine("Enter a command:");

    var command = Console.ReadLine();

    if (command == "exit")
    {
        break;
    }
}