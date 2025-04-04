using System.Net.WebSockets;

namespace SimpleStreamingApi;

public struct WebSocketChunk
{
     public byte[] Data { get; init; }
     public int Length { get; init; }
     public WebSocketMessageType MessageType { get; init; }
     public bool EndOfMessage { get; init; }
}