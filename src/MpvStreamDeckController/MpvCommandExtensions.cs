using BarRaider.SdTools;
using System.Runtime.CompilerServices;

namespace MpvStreamDeckController
{
    public static class MpvCommandExtensions
    {
        public static async Task SendCommandsAsync(this ISDConnection connection, IEnumerable<MpvCommand>? commands, [CallerMemberName] string caller = "")
        {
            try
            {
                var localCommands = commands;

                if (await MpvClient.Instance.SendAllAsync(localCommands))
                {
                    return;
                }

                await connection.ShowAlert();
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"{caller} failed: {ex.Message}");
            }
        }
    }
}
