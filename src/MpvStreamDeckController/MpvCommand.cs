using System.IO.Pipes;

namespace MpvStreamDeckController
{
    public static class MpvCommand
    {
        private const string PipeName = "mpvsocket";

        private static readonly MpvClient Client = new(ConnectPipeAsync);

        public static event Action<MpvState>? StateChanged
        {
            add => Client.StateChanged += value;
            remove => Client.StateChanged -= value;
        }
 
        public static MpvState State => Client.State;
 
        public static IDisposable ObserveProperty(IReadOnlyCollection<MpvProperty> properties) => Client.ObserveProperty(properties);

        public static Task<bool> SendCommandAsync(object[] command) => Client.SendAsync(command);        
        public static void NotifyApplicationLaunched() => Client.NotifyLaunched();

        private static async Task<Stream?> ConnectPipeAsync(TimeSpan timeout, CancellationToken token)
        {
            var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            
            try
            {
                await pipe.ConnectAsync((int)timeout.TotalMilliseconds, token);
                return pipe;
            }
            catch (Exception)
            {
                pipe.Dispose();
                token.ThrowIfCancellationRequested();
                return null; 
            }
        }        
    }
}