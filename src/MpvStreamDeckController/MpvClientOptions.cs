namespace MpvStreamDeckController
{
    public record MpvClientOptions
    {
        /// <summary>
        /// How long to wait for a successful shutdown on disposal.
        /// </summary>
        public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How long a single attempt to connect to mpv may take. Also limits how long a send made while disconnected
        /// waits for a reconnect.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Interval between connection retry attempts.
        /// </summary>
        public TimeSpan ConnectionRetryInterval { get; init; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// How long to keep retrying an unreachable mpv after a launch event or a connect request (e.g. a send made
        /// while disconnected), retrying every <see cref="ConnectionRetryInterval"/>.
        /// </summary>
        public TimeSpan LaunchRetryWindow { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How long to wait for mpv's reply to a command before treating the outcome as no reply.
        /// </summary>
        public TimeSpan ReplyTimeout { get; init; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How long a single write to mpv's pipe may take. A write that takes longer means mpv has stopped reading, so
        /// the connection is closed.
        /// </summary>
        public TimeSpan WriteTimeout { get; init; } = TimeSpan.FromSeconds(2);
    }
}