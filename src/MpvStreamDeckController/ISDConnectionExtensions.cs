using System.Runtime.CompilerServices;
using BarRaider.SdTools;

namespace MpvStreamDeckController;

public static class ISDConnectionExtensions
{
    extension(ISDConnection connection)
    {
        public async Task SendCommandsAsync(IEnumerable<MpvCommand>? commands, [CallerMemberName] string caller = "")
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
