using System.Runtime.CompilerServices;
using BarRaider.SdTools;

namespace MpvStreamDeckController;

public static class ISDConnectionExtensions
{
    extension(ISDConnection connection)
    {
        /// <summary>
        /// Works out the commands and sends them to mpv. Shows the alert on the key or dial if they could not be sent,
        /// were rejected, <paramref name="getCommands"/> returns null (the action has nothing valid to send), or
        /// anything throws, which is also logged.
        /// <para>
        /// Nothing escapes this method, including an exception from <paramref name="getCommands"/>: the Stream Deck
        /// library does not catch exceptions from an action's event handlers, so one would end the plugin process.
        /// </para>
        /// </summary>
        /// <param name="mpv">The mpv client to send them through.</param>
        /// <param name="getCommands">Returns the commands to send, or null if the action has none. Called once, before this method first yields.</param>
        /// <param name="action">Name of the action sending them, for the log.</param>
        /// <param name="caller">The event that triggered the send, for the log; filled in by the compiler.</param>
        public async Task SendCommandsAsync(IMpvClient mpv, Func<IEnumerable<MpvCommand>?> getCommands, string action, [CallerMemberName] string caller = "")
        {
            try
            {
                var commands = getCommands();

                if (commands is null || (await mpv.SendAsync(commands)).IsFailure)
                {
                    await connection.ShowAlert();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"{action}.{caller} failed: {ex}");

                try
                {
                    await connection.ShowAlert();
                }
                catch (Exception alertException)
                {
                    LogHelper.Log(TracingLevel.ERROR, $"{action}.{caller}: showing the alert failed: {alertException}");
                }
            }
        }
    }
}
