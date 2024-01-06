using Mediatr.Broker;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMediatorServer>(new MediatorServer(3333, "127.0.0.1", "MediatorServer"));


var app = builder.Build();
var broker = app.Services.GetRequiredService<IMediatorServer>();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

broker.StartAsync();


app.Run();