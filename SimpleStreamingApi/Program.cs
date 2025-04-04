using SimpleStreamingApi;
using SimpleStreamingApi.Managers;
using SimpleStreamingApi.Middleware;
using SimpleStreamingApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<StreamManager>();
builder.Services.AddHostedService<BroadcastService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseWebSockets();

app.UseMiddleware<WebSocketStreamMiddleware>();

app.Run();