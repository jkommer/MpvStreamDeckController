using BarRaider.SdTools;
using System.Text;

namespace MpvStreamDeckController
{
    public record MpvClientOptions
    {
        public TimeSpan MessageTimeout { get; init; } = TimeSpan.FromMilliseconds(250);        
    }

    public class MpvClient(MpvClient.Connector connect, MpvClientOptions? options = null)
    {
        public delegate Task<Stream?> Connector(TimeSpan timeout, CancellationToken token);
        private readonly Connector _connect = connect;
        public readonly MpvClientOptions _options = options ?? new MpvClientOptions();

        private readonly SemaphoreSlim _messageLock = new(1, 1);

        public async Task<bool> SendAsync(object[] command)
        {
            var bytes = Encoding.UTF8.GetBytes(MpvProtocol.BuildCommand(command));

            await _messageLock.WaitAsync();

            try
            {
                using var stream = await _connect(_options.MessageTimeout, CancellationToken.None);
                
                if (stream is null)
                {
                    LogHelper.Log(TracingLevel.WARN, "mpv is not reachable; command dropped.");
                    return false;
                }

                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.WARN, $"Error sending mpv command: {ex.Message}");
                return false;
            }
            finally
            {
                _messageLock.Release();
            }
        }
    }
}