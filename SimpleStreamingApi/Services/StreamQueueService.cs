using System.Threading.Channels;

namespace SimpleStreamingApi.Services;

public class StreamQueueService<T>(ILogger<StreamQueueService<T>> logger)
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public async Task WriteNextItemAsync(T item)
    {
        logger.LogTrace("WriteNextByteSegment");
        await _channel.Writer.WriteAsync(item);
    }

    public async Task<T> ReadNextItemAsync()
    {
        logger.LogTrace("ReadNextByteSegment");
        return await _channel.Reader.ReadAsync();
    }

    public ValueTask<bool> HasNextItemAsync()
    {
        return _channel.Reader.WaitToReadAsync();
    }
    
    
}