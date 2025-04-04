using VideoForwardingApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseWebSockets(new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseMiddleware<WebsocketMiddleware>();

app.Run();