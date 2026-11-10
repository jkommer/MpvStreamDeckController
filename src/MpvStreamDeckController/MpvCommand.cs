using MpvStreamDeckController.Actions.Settings;

namespace MpvStreamDeckController
{
    public record MpvCommand(params object[] Components)
    {
        /// <summary>
        /// When true the command is sent with the expand-properties prefix so ${property} references are expanded.
        /// </summary>
        public bool ExpandProperties { get; init; }

        /// <summary>
        /// OSD prefix to send with the command. Null leaves it as mpv's default.
        /// </summary>
        public MpvOsdMode? Osd { get; init; }

        public static implicit operator MpvCommand(object[] args) => new(args);
    }
}