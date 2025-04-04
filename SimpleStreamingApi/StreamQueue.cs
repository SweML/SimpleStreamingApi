using System.Text;
using System.Text.Unicode;
using System.Threading.Channels;
using Matroska;

namespace SimpleStreamingApi;

public class StreamQueue<T>(ILogger<StreamQueue<T>> logger)
{
    private readonly ILogger<StreamQueue<T>> _logger = logger;
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public async Task WriteNextItemAsync(T item)
    {
        _logger.LogTrace("WriteNextByteSegment");
        await _channel.Writer.WriteAsync(item);
    }

    public async Task<T> ReadNextItemAsync()
    {
        _logger.LogTrace("ReadNextByteSegment");
        return await _channel.Reader.ReadAsync();
    }

    public ValueTask<bool> HasNextItemAsync()
    {
        return _channel.Reader.WaitToReadAsync();
    }
    
    
}