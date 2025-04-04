using System.Net.WebSockets;
using Matroska;

namespace SimpleStreamingApi;

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
                var streamerId = query["streamer"]!;
                var queue = streamManager.GetOrCreateStream(streamerId);
                await HandleStreamer(webSocket, queue, streamerId).ConfigureAwait(false);
            }
            else if (query.ContainsKey("viewer"))
            {
                var streamerId = query["viewer"]!;
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
    }

    private async Task HandleStreamer(WebSocket socket, StreamQueue<WebSocketChunk> queue, string streamerId)
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

                await queue.WriteNextItemAsync(chunk);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in HandleStreamer");
        }
        // Close the socket
        if (socket.State != WebSocketState.Closed)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Streamer done", CancellationToken.None);
        }

        // Optional: Remove stream and viewers
        streamManager.RemoveStream(streamerId);

        logger.LogInformation("Streamer WebSocket closed");
    }
}