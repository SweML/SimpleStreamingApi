using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VideoForwardingApi;

public class WebsocketMiddleware(RequestDelegate next)
{
    private readonly StreamSession _session = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/ws")
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            if (context.Request.Query.ContainsKey("watcher"))
            {
                _session.ConnectWatcher(webSocket);
            }
            else
            {
                _session.ConnectStreamer(webSocket);
            }

            // Keep connection open while it's valid
            while (webSocket.State == WebSocketState.Open)
            {
                await Task.Delay(100);
            }
        }
        else
        {
            await next(context);
        }
    }
}