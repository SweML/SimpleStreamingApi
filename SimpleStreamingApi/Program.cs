using SimpleStreamingApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<StreamManager>();
builder.Services.AddHostedService<BroadcastService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseWebSockets(new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseMiddleware<WebSocketStreamMiddleware>();

app.Run();