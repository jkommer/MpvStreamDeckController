using System.Runtime.CompilerServices;
using BarRaider.SdTools;
using MpvStreamDeckController;

[assembly: InternalsVisibleTo("MpvStreamDeckController.Tests")]

try
{
    // Blocks until Stream Deck disconnects from the plugin.
    SDWrapper.Run(args);
}
finally
{
    MpvClient.Shutdown();
}
