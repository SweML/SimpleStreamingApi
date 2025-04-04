using SimpleStreamingApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<StreamManager>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseWebSockets(new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseMiddleware<WebSocketMiddleware>();

app.Run();