namespace SimpleStreamingApi;

public class BroadcastService(StreamManager streamManager, ILogger<BroadcastService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StreamBroadcastService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var streamerId in streamManager.GetActiveStreamerIds())
            {
                var streamQueue = streamManager.GetStreamQueue(streamerId);

                if (streamQueue == null)
                    continue;

                while (await streamQueue.HasNextItemAsync())
                {
                    var chunk = await streamQueue.ReadNextItemAsync();
                    await streamManager.BroadcastToViewers(streamerId, chunk);
                }
            }

            await Task.Delay(10, stoppingToken);
        }

        logger.LogInformation("StreamBroadcastService stopped");
    }
}
