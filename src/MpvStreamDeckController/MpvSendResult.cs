namespace MpvStreamDeckController;

public enum MpvSendStatus
{
    /// <summary>mpv ran the command.</summary>
    Success,

    /// <summary>mpv replied with an error.</summary>
    Rejected,

    /// <summary>Not connected to mpv; the command was dropped.</summary>
    NotConnected,

    /// <summary>Writing to the pipe failed.</summary>
    WriteFailed,

    /// <summary>The command was sent but no reply arrived in time, or the connection closed first. The outcome is unknown: some commands legitimately never reply promptly.</summary>
    NoReply,
}

/// <summary>The outcome of sending a command to mpv.</summary>
/// <param name="Status">What happened to the command.</param>
/// <param name="Error">mpv's error message when <see cref="Status"/> is <see cref="MpvSendStatus.Rejected"/>.</param>
public readonly record struct MpvSendResult(MpvSendStatus Status, string? Error = null)
{
    public static MpvSendResult Success => new(MpvSendStatus.Success);
    public static MpvSendResult NotConnected => new(MpvSendStatus.NotConnected);
    public static MpvSendResult WriteFailed => new(MpvSendStatus.WriteFailed);
    public static MpvSendResult NoReply => new(MpvSendStatus.NoReply);
    public static MpvSendResult Rejected(string error) => new(MpvSendStatus.Rejected, error);

    /// <summary>True when the command definitely did not run. <see cref="MpvSendStatus.NoReply"/> is not a failure.</summary>
    public bool IsFailure => Status is MpvSendStatus.Rejected or MpvSendStatus.NotConnected or MpvSendStatus.WriteFailed;
}
