using System.Net.WebSockets;

namespace SimpleStreamingApi;

public class StreamSession
{
    private WebSocket? streamer;
    private WebSocket? viewer;
    private int MaxByteSize = 0;
    private byte[] Header;
    private bool HeaderStored = false;

    public async Task ConnectViewer(WebSocket viewer)
    {
        this.viewer = viewer;

        if (HeaderStored && viewer?.State == WebSocketState.Open)
        {
            Console.WriteLine("Sending header");
            await viewer.SendAsync(
                new ArraySegment<byte>(Header),  // Send the data array, not the buffer
                WebSocketMessageType.Binary,
                false,
                CancellationToken.None);
        }
    }

    public void ConnectStreamer(WebSocket streamer)
    {
        this.streamer = streamer;
        _ = StartStreaming(); // Start forwarding when the streamer connects
    }

    private async Task StartStreaming()
    {
        var buffer = new byte[1024 * 1024]; // 1 Mb buffer
        try
        {
            while (streamer?.State == WebSocketState.Open)
            {
                var result = await streamer.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var data = new byte[result.Count];
                    Array.Copy(buffer, data, result.Count);

                    if (!HeaderStored)
                    {
                        Header = data.ToArray();
                        HeaderStored = true;
                    }

                    // Process the video data here (e.g., save or send it to a client)

                    if (data.Length > MaxByteSize)
                    {
                        MaxByteSize = data.Length;
                        Console.WriteLine($"Biggest received video chunk size is: {MaxByteSize} bytes");
                    }

                    if (viewer?.State == WebSocketState.Open)
                    {
                        await viewer.SendAsync(
                            new ArraySegment<byte>(data),  // Send the data array, not the buffer
                            result.MessageType,
                            result.EndOfMessage,
                            CancellationToken.None);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        finally
        {
            await CloseSockets();
        }
    }


    private async Task CloseSockets()
    {
        if (streamer != null && streamer.State != WebSocketState.Closed)
            await streamer.CloseAsync(WebSocketCloseStatus.NormalClosure, "Streaming ended", CancellationToken.None);
        
        if (viewer != null && viewer.State != WebSocketState.Closed)
            await viewer.CloseAsync(WebSocketCloseStatus.NormalClosure, "Streaming ended", CancellationToken.None);
    }
}