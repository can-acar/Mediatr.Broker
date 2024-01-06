using client.slave;
using Mediatr.Broker;

var builder = WebApplication.CreateBuilder(args);

// add IHandler, IRequest, IRequest<,>  to the DI container
builder.Services.AddSingleton<IMediator, Mediator>();

builder.Services.AddSingleton<IMediatorClient>(new MediatorClient(3333, "127.0.0.1", "slave"));

builder.Services.RegisterHandler<SampleRequest, SampleHandler>();

// mediator.RegisterHandler<SampleHandler, SampleRequest>();

var app = builder.Build();

var client = app.Services.GetService<IMediatorClient>();


client?.Register();

client?.Run();


app.MapGet("/", () => "Hello World!");

app.Run();