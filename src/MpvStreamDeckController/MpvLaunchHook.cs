using BarRaider.SdTools;

namespace MpvStreamDeckController;

/// <summary>
/// Wakes the idle mpv connection when Stream Deck reports an application launch
/// (manifest <c>ApplicationsToMonitor</c> should list only mpv.exe).
///
/// Each action's own <see cref="ISDConnection"/> forwards that event and stops forwarding when the action is
/// disposed, so subscribing on it would silently stop working as soon as that action left the screen. The shared
/// <c>StreamDeckConnection</c> behind it lives as long as the plugin, so we subscribe there, once.
/// </summary>
public static class MpvLaunchHook
{
    private static int _attached;

    /// <summary>Call from every action's constructor; only the first call does anything.</summary>
    public static void EnsureAttached(ISDConnection connection)
    {
        // A connection without the shared connection behind it only occurs in tests, where there is nothing to attach to.
        if (connection.StreamDeckConnection is not { } sharedConnection)
        {
            return;
        }

        if (Interlocked.Exchange(ref _attached, 1) == 0)
        {
            sharedConnection.OnApplicationDidLaunch += (_, _) => MpvClient.Instance.NotifyMpvLaunched();
        }
    }
}
