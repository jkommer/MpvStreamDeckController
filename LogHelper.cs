using BarRaider.SdTools;

namespace MpvStreamDeckController
{
    public static class LogHelper
    {
        public static void Log(TracingLevel level, string message)
        {
            try
            {
                Logger.Instance.LogMessage(level, message);
            }
            catch
            {
            }
        }
    }
}