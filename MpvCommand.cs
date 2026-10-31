using System.IO.Pipes;

namespace MpvStreamDeckController
{
    public static class MpvCommand
    {
        private const string PipeName = "mpvsocket";

        private static readonly MpvClient Client = new(ConnectPipeAsync);

        public static Task<bool> SendCommandAsync(object[] command) => Client.SendAsync(command);

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
