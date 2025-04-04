using System.Net.WebSockets;

namespace SimpleStreamingApi;

public static class WebSocketChunkExtensions
{

    public static WebSocketChunk ToWebSocketChunk(this byte[] bytes, WebSocketReceiveResult result)
    {
        return new WebSocketChunk()
        {
            Data = bytes, 
            Length = result.Count, 
            MessageType = result.MessageType, 
            EndOfMessage = result.EndOfMessage
        };
    }
    
}