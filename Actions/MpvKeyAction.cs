using BarRaider.SdTools;

namespace MpvStreamDeckController.Actions
{
    public abstract class MpvKeyAction : KeypadBase
    {
        protected abstract object[]? Command { get; }

        protected MpvKeyAction(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            MpvLaunchHook.EnsureAttached(connection);
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            try
            {
                var command = Command;

                if (command is null || !await MpvCommand.SendCommandAsync(command))
                {
                    await Connection.ShowAlert();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"KeyPressed failed: {ex.Message}");
            }
        }

        public override void KeyReleased(KeyPayload payload) { }
        public override void OnTick() { }
        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }
        public override void ReceivedSettings(ReceivedSettingsPayload payload) { }
        public override void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}