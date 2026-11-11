using BarRaider.SdTools;
using MpvStreamDeckController.Actions;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class MpvKeyActionTests
{
    private readonly FakeSdConnection _connection = new();
    private readonly FakeMpvClient _mpv = new();

    [Fact]
    public void KeyPressed_SendsTheCommands()
    {
        using var action = CreateAction();
        action.CommandsToSend = [new("cycle", "pause"), new("seek", 5)];

        action.KeyPressed(null!);

        _mpv.Sent.Count.ShouldBe(1);

        _mpv.Sent[0]
            .Select(c => c.DecodedCommandJson())
            .ShouldBe(["[\"cycle\",\"pause\"]", "[\"seek\",5]"]);

        _connection.Alerts.ShouldBe(0);
    }

    [Fact]
    public void KeyPressed_ReadsTheCommandsAtEachPress()
    {
        using var action = CreateAction();

        action.CommandsToSend = [new("cycle", "pause")];
        action.KeyPressed(null!);
        action.CommandsToSend = [new("cycle", "mute")];
        action.KeyPressed(null!);

        _mpv.Sent
            .Select(batch => batch.Single().DecodedCommandJson())
            .ShouldBe(["[\"cycle\",\"pause\"]", "[\"cycle\",\"mute\"]"]);
    }

    [Fact]
    public void KeyPressed_WithNoCommands_ShowsTheAlertAndSendsNothing()
    {
        using var action = CreateAction();
        action.CommandsToSend = null;

        action.KeyPressed(null!);

        _mpv.Sent.ShouldBeEmpty();
        _connection.Alerts.ShouldBe(1);
    }

    [Fact]
    public void KeyPressed_WhenTheSendFails_ShowsTheAlert()
    {
        using var action = CreateAction();
        _mpv.SendResult = MpvSendResult.NotConnected;

        action.KeyPressed(null!);

        _connection.Alerts.ShouldBe(1);
    }

    [Fact]
    public void KeyPressed_WhenMpvDoesNotReply_DoesNotShowTheAlert()
    {
        using var action = CreateAction();
        _mpv.SendResult = MpvSendResult.NoReply;

        action.KeyPressed(null!);

        _connection.Alerts.ShouldBe(0);
    }

    [Fact]
    public void KeyPressed_AppliesTheOsdSettingTheActionWasCreatedWith()
    {
        using var action = CreateAction(settings: new() { ["osd"] = "osd-bar" });

        action.KeyPressed(null!);

        _mpv.Sent.Single().Single().Osd.ShouldBe(MpvOsdMode.OsdBar);
    }

    [Fact]
    public void KeyPressed_WithoutAnOsdSetting_LeavesTheCommandsAlone()
    {
        using var action = CreateAction();

        action.KeyPressed(null!);

        _mpv.Sent.Single().Single().Osd.ShouldBeNull();
    }

    [Fact]
    public void ReceivedSettings_UpdatesTheOsdSettingAndTellsTheAction()
    {
        using var action = CreateAction(settings: new() { ["osd"] = "osd-bar" });
        var newSettings = new JObject { ["osd"] = "no-osd" };

        action.ReceivedSettings(TestPayloads.Settings(newSettings));
        action.KeyPressed(null!);

        _mpv.Sent
            .Single()
            .Single().Osd
            .ShouldBe(MpvOsdMode.NoOsd);

        JToken.DeepEquals(action.SettingsReceived.ShouldHaveSingleItem(), newSettings).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithoutObservedProperties_ObservesNothingAndShowsNothing()
    {
        using var action = CreateAction();

        _mpv.ObservedProperties.ShouldBeEmpty();
        action.FeedbackRequests.ShouldBe(0);
        _connection.ImageStates.ShouldBeEmpty();
        _connection.Titles.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ObservesThePropertiesUntilDisposed()
    {
        var action = CreateAction(MpvProperties.Pause, MpvProperties.Mute);

        _mpv.ObservedProperties.ShouldBe([MpvProperties.Pause, MpvProperties.Mute]);

        action.Dispose();

        _mpv.ObservedProperties.ShouldBeEmpty();
    }

    [Fact]
    public void StateChange_ShowsTheFeedbackOnTheKey()
    {
        using var action = CreateAction(MpvProperties.Pause);
        action.Feedback = state => new(state.Flag(MpvProperties.Pause) == true ? 1u : 0u, state.IsConnected ? "on" : "off");

        _mpv.Raise(TestStates.Connected((MpvProperties.Pause, true)));

        _connection.ImageStates.ShouldBe([1u]);
        _connection.Titles.ShouldBe(["on"]);
    }

    [Fact]
    public void StateChange_OnlySetsThePartsOfTheKeyTheFeedbackNames()
    {
        using var action = CreateAction(MpvProperties.Pause);
        action.Feedback = _ => new(ImageState: 1);

        _mpv.Raise(TestStates.Connected());

        _connection.ImageStates.ShouldBe([1u]);
        _connection.Titles.ShouldBeEmpty();
    }

    [Fact]
    public void StateChange_WhenTheFeedbackIsUnchanged_DoesNotUpdateTheKeyAgain()
    {
        using var action = CreateAction(MpvProperties.Pause);
        action.Feedback = _ => new(ImageState: 1);

        _mpv.Raise(TestStates.Connected(version: 1));
        _mpv.Raise(TestStates.Connected(version: 2));

        _connection.ImageStates.ShouldBe([1u]);
    }

    [Fact]
    public void StateChange_AfterDispose_IsIgnored()
    {
        var action = CreateAction(MpvProperties.Pause);
        action.Feedback = _ => new(ImageState: 1);
        action.Dispose();

        _mpv.Raise(TestStates.Connected());

        _connection.ImageStates.ShouldBeEmpty();
    }

    [Fact]
    public void State_IsTheLatestStateForAnActionThatObservesProperties()
    {
        using var action = CreateAction(MpvProperties.Pause);
        var state = TestStates.Connected((MpvProperties.Pause, true));

        action.CurrentState.ShouldBeSameAs(MpvState.Inactive);

        _mpv.Raise(state);

        action.CurrentState.ShouldBeSameAs(state);
    }

    private TestKeyAction CreateAction(params MpvProperty[] observedProperties) => CreateAction(null, observedProperties);

    private TestKeyAction CreateAction(JObject? settings, params MpvProperty[] observedProperties)
        => new(_connection, TestPayloads.Initial(settings), _mpv, observedProperties);

    /// <summary>A key action whose commands and feedback the test controls.</summary>
    private sealed class TestKeyAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty> observedProperties)
        : MpvKeyAction(connection, payload, mpv, observedProperties)
    {
        public MpvCommand[]? CommandsToSend { get; set; } = [new("cycle", "pause")];

        public Func<MpvState, KeyFeedback> Feedback { get; set; } = _ => default;

        public int FeedbackRequests { get; private set; }

        public List<JObject?> SettingsReceived { get; } = [];

        public MpvState CurrentState => State;

        protected override MpvCommand[]? Commands => CommandsToSend;

        protected override KeyFeedback GetFeedback(MpvState state)
        {
            FeedbackRequests++;
            return Feedback(state);
        }

        protected override void OnSettingsChanged(JObject? settings) => SettingsReceived.Add(settings);
    }
}
