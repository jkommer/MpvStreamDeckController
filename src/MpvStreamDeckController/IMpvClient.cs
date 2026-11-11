namespace MpvStreamDeckController;

/// <summary>What an action needs from mpv: its state, and a way to send it commands.</summary>
public interface IMpvClient : IMpvStateSource
{
    /// <summary>
    /// Sends the commands one at a time and stops at the first failure.
    /// </summary>
    /// <returns>The first failure, or else the last command's result (<see cref="MpvSendResult.Success"/> if there were none).</returns>
    Task<MpvSendResult> SendAsync(IEnumerable<MpvCommand> commands);
}
