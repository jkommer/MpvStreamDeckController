namespace MpvStreamDeckController;

/// <summary>A message received from mpv that we act on.</summary>
public abstract record MpvMessage;

/// <summary>A property-change event for an observed property.</summary>
/// <param name="Value">A double, bool, string, JsonElement (arrays/objects), or null when unavailable.</param>
public sealed record MpvPropertyChange(long PropertyId, object? Value) : MpvMessage;

/// <summary>The reply to a command.</summary>
/// <param name="RequestId">The request_id we sent, or 0 if we sent none.</param>
/// <param name="Error">"success", or mpv's error message.</param>
public sealed record MpvReply(long RequestId, string Error) : MpvMessage
{
    public bool IsSuccess => Error == "success";
}
