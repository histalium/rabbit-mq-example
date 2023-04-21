namespace RabbitMQExample;

public static class CancellationTokenExtensions
{
    public static async Task WaitTillCanceled(this CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(int.MaxValue, token);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
