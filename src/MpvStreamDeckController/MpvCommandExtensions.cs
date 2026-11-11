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
                if (commands is null || (await MpvClient.Instance.SendAsync(commands)).IsFailure)
                {
                    await connection.ShowAlert();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"{caller} failed: {ex}");
            }
        }
    }
}
