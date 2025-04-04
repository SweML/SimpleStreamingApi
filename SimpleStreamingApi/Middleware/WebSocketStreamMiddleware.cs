using System.Net.WebSockets;
using SimpleStreamingApi.Extensions;
using SimpleStreamingApi.Managers;
using SimpleStreamingApi.Models;
using SimpleStreamingApi.Services;

namespace SimpleStreamingApi.Middleware;

public class WebSocketStreamMiddleware(RequestDelegate next, StreamManager streamManager, ILogger<WebSocketStreamMiddleware> logger)
{
    
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
            var query = context.Request.Query;

            if (query.ContainsKey("streamer"))
            {
                var streamerId = query["streamer"].ToString();
                var queue = streamManager.GetOrCreateStream(streamerId);
                await HandleStreamer(webSocket, queue, streamerId).ConfigureAwait(false);
            }
            else if (query.ContainsKey("viewer"))
            {
                var streamerId = query["viewer"].ToString();
                if (streamManager.ConnectViewerToStream(streamerId, webSocket))
                {
                    await WaitForSocket(webSocket);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }
        }
        else
        {
            await next(context);
        }
    }

    private async Task WaitForSocket(WebSocket webSocket)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            await Task.Delay(100);
        }
        logger.LogInformation("Websocket connection closed.");
    }

    private async Task HandleStreamer(WebSocket socket, StreamQueueService<WebSocketChunk> queueService, string streamerId)
    {
        var buffer = new byte[1024 * 1024];
        try
        {
            logger.LogInformation("Streamer connected");

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("Streamer initiated close");
                    break;
                }

                var byteChunk = buffer[..result.Count];
                WebSocketChunk chunk = byteChunk.ToWebSocketChunk(result);
                
                logger.LogTrace("Received segment: {Length} bytes", result.Count);

                await queueService.WriteNextItemAsync(chunk);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in HandleStreamer");
        }
        
        if (socket.State != WebSocketState.Closed)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Streamer done", CancellationToken.None);
        }

        await streamManager.RemoveStream(streamerId);

        logger.LogInformation("Streamer WebSocket closed");
    }
}