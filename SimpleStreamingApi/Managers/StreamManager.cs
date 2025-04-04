using System.Collections.Concurrent;
using System.Net.WebSockets;
using SimpleStreamingApi.Models;
using SimpleStreamingApi.Services;

namespace SimpleStreamingApi.Managers;

public class StreamManager(ILoggerFactory loggerFactory)
{
    private readonly ConcurrentDictionary<string, StreamQueueService<WebSocketChunk>> _streams = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _clients = new();
    private readonly ILogger<StreamManager> _logger = loggerFactory.CreateLogger<StreamManager>();
    
    public StreamQueueService<WebSocketChunk> GetOrCreateStream(string streamerId)
    {
        _logger.LogInformation("Creating stream for {streamerId}", streamerId);
        return _streams.GetOrAdd(streamerId, new StreamQueueService<WebSocketChunk>(loggerFactory.CreateLogger<StreamQueueService<WebSocketChunk>>()));
    }
    
    public IEnumerable<string> GetActiveStreamerIds() => _streams.Keys;

    
    public StreamQueueService<WebSocketChunk>? GetStreamQueue(string streamerId)
    {
        _streams.TryGetValue(streamerId, out var queue);
        return queue;
    }
    
    public async Task BroadcastToViewers(string streamerId, WebSocketChunk chunk)
    {
        if (_clients.TryGetValue(streamerId, out var sockets))
        {
            foreach (var socket in sockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(chunk.Data, chunk.MessageType, chunk.EndOfMessage, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send chunk to viewer.");
                    }
                }
            }
        }
    }

    public bool ConnectViewerToStream(string streamerId, WebSocket viewer)
    {
        _logger.LogInformation("Connecting viewer to stream: {streamerId}", streamerId);
        if (!_streams.ContainsKey(streamerId))
            return false;
        
        var viewers = _clients.GetOrAdd(streamerId, _ => new ConcurrentBag<WebSocket>());
        viewers.Add(viewer);
        return true;
    }

    public async Task RemoveStream(string streamerId)
    {
        _streams.TryRemove(streamerId, out _);
        _clients.TryRemove(streamerId, out var bag);
        _logger.LogInformation("Removed stream and clients for {streamerId}", streamerId);

        if (bag == null)
            return;
        
        foreach (var client in bag)
        {
            if (client.State == WebSocketState.Open)
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }
        {
            
        }
    }
}
