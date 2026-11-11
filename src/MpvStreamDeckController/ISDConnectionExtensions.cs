using System.Runtime.CompilerServices;
using BarRaider.SdTools;

namespace MpvStreamDeckController;

public static class ISDConnectionExtensions
{
    extension(ISDConnection connection)
    {
        /// <summary>
        /// Sends the commands to mpv and shows the alert on the key or dial if they could not be sent, were rejected, or
        /// <paramref name="commands"/> is null (the action has nothing valid to send).
        /// </summary>
        /// <param name="mpv">The mpv client to send them through.</param>
        /// <param name="commands">The commands to send, or null if the action has none.</param>
        /// <param name="action">Name of the action sending them, for the log.</param>
        /// <param name="caller">The event that triggered the send, for the log; filled in by the compiler.</param>
        public async Task SendCommandsAsync(IMpvClient mpv, IEnumerable<MpvCommand>? commands, string action, [CallerMemberName] string caller = "")
        {
            try
            {
                if (commands is null || (await mpv.SendAsync(commands)).IsFailure)
                {
                    await connection.ShowAlert();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"{action}.{caller} failed: {ex}");
            }
        }
    }
}
