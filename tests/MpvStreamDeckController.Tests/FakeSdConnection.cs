using BarRaider.SdTools;
using BarRaider.SdTools.Communication;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;

namespace MpvStreamDeckController.Tests;

/// <summary>
/// An <see cref="ISDConnection"/> for action tests: records what an action shows on its key or dial. Everything else
/// does nothing. All calls complete immediately.
/// </summary>
internal sealed class FakeSdConnection : ISDConnection
{
    /// <summary>How many times the alert was shown.</summary>
    public int Alerts { get; private set; }

    /// <summary>Every image state set on the key, in order.</summary>
    public List<uint> ImageStates { get; } = [];

    /// <summary>Every title set on the key, in order.</summary>
    public List<string> Titles { get; } = [];

    /// <summary>Every set of dial feedback values sent, in order.</summary>
    public List<Dictionary<string, string>> Feedback { get; } = [];

    public Task ShowAlert()
    {
        Alerts++;
        return Task.CompletedTask;
    }

    public Task SetStateAsync(uint state)
    {
        ImageStates.Add(state);
        return Task.CompletedTask;
    }

    public Task SetTitleAsync(string title, int? state = null)
    {
        Titles.Add(title);
        return Task.CompletedTask;
    }

    public Task SetFeedbackAsync(Dictionary<string, string> dictKeyValue)
    {
        Feedback.Add(new(dictKeyValue));
        return Task.CompletedTask;
    }

    // Everything below is unused by the actions under test.

    public StreamDeckConnection StreamDeckConnection => null!;
    public string ContextId => "test-context";
    public string DeviceId => "test-device";

#pragma warning disable CS0067 // The events are part of the interface but never raised here.
    public event EventHandler<SDEventReceivedEventArgs<ApplicationDidLaunch>>? OnApplicationDidLaunch;
    public event EventHandler<SDEventReceivedEventArgs<ApplicationDidTerminate>>? OnApplicationDidTerminate;
    public event EventHandler<SDEventReceivedEventArgs<DeviceDidConnect>>? OnDeviceDidConnect;
    public event EventHandler<SDEventReceivedEventArgs<DeviceDidDisconnect>>? OnDeviceDidDisconnect;
    public event EventHandler<SDEventReceivedEventArgs<PropertyInspectorDidAppear>>? OnPropertyInspectorDidAppear;
    public event EventHandler<SDEventReceivedEventArgs<PropertyInspectorDidDisappear>>? OnPropertyInspectorDidDisappear;
    public event EventHandler<SDEventReceivedEventArgs<SendToPlugin>>? OnSendToPlugin;
    public event EventHandler<SDEventReceivedEventArgs<SystemDidWakeUp>>? OnSystemDidWakeUp;
    public event EventHandler<SDEventReceivedEventArgs<TitleParametersDidChange>>? OnTitleParametersDidChange;

#pragma warning restore CS0067

    public StreamDeckDeviceInfo DeviceInfo() => null!;
    public Task GetGlobalSettingsAsync() => Task.CompletedTask;
    public Task GetSettingsAsync() => Task.CompletedTask;
    public Task LogSDMessage(string message) => Task.CompletedTask;
    public Task OpenUrlAsync(string uri) => Task.CompletedTask;
    public Task OpenUrlAsync(Uri uri) => Task.CompletedTask;
    public Task SendToPropertyInspectorAsync(JObject settings) => Task.CompletedTask;
    public Task SetDefaultImageAsync() => Task.CompletedTask;
    public Task SetFeedbackAsync(JObject feedbackPayload) => Task.CompletedTask;
    public Task SetFeedbackAsync(string feedbackItem, string value) => Task.CompletedTask;
    public Task SetFeedbackLayoutAsync(string layout) => Task.CompletedTask;
    public Task SetGlobalSettingsAsync(JObject settings, bool triggerDidReceiveGlobalSettings = true) => Task.CompletedTask;
    public Task SetImageAsync(byte[] image, int? state = null, bool forceSendToStreamdeck = false) => Task.CompletedTask;
    public Task SetImageAsync(SkiaSharp.SKBitmap image, int? state = null, bool forceSendToStreamdeck = false) => Task.CompletedTask;
    public Task SetImageAsync(System.Drawing.Image image, int? state = null, bool forceSendToStreamdeck = false) => Task.CompletedTask;
    public Task SetImageAsync(string base64Image, int? state = null, bool forceSendToStreamdeck = false) => Task.CompletedTask;
    public Task SetSettingsAsync(JObject settings) => Task.CompletedTask;
    public Task ShowOk() => Task.CompletedTask;
    public Task SwitchProfileAsync(string profileName) => Task.CompletedTask;
    public Task SwitchProfileAsync(string profileName, string deviceId) => Task.CompletedTask;

    public void Dispose()
    {
    }
}
