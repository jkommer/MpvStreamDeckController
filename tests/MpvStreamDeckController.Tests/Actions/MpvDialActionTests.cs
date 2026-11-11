using BarRaider.SdTools;
using MpvStreamDeckController.Actions;
using MpvStreamDeckController.Actions.Settings;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace MpvStreamDeckController.Tests.Actions;

public class MpvDialActionTests
{
    private readonly FakeSdConnection _connection = new();
    private readonly FakeMpvClient _mpv = new();

    [Theory]
    [InlineData(3)]
    [InlineData(-2)]
    public void DialRotate_SendsTheRotateCommandsForTheTicks(int ticks)
    {
        using var action = CreateAction();

        action.DialRotate(TestPayloads.Rotate(ticks));

        _mpv.Sent
            .Single()
            .Single()
            .DecodedCommandJson()
            .ShouldBe($"[\"rotate\",{ticks}]");

        _connection.Alerts.ShouldBe(0);
    }

    [Fact]
    public void DialDown_SendsThePressCommands()
    {
        using var action = CreateAction();

        action.DialDown(null!);

        _mpv.Sent
            .Single()
            .Single()
            .DecodedCommandJson()
            .ShouldBe("[\"press\"]");
    }

    [Fact]
    public void TouchPress_SendsThePressCommands()
    {
        using var action = CreateAction();

        action.TouchPress(null!);

        _mpv.Sent
            .Single()
            .Single()
            .DecodedCommandJson()
            .ShouldBe("[\"press\"]");
    }

    [Fact]
    public void DialUp_SendsNothing()
    {
        using var action = CreateAction();

        action.DialUp(null!);

        _mpv.Sent.ShouldBeEmpty();
    }

    [Fact]
    public void DialRotate_WhenTheSendFails_ShowsTheAlert()
    {
        using var action = CreateAction();
        _mpv.SendResult = MpvSendResult.Rejected("error running command");

        action.DialRotate(TestPayloads.Rotate(1));

        _connection.Alerts.ShouldBe(1);
    }

    [Fact]
    public void Commands_UseTheOsdSettingTheActionWasCreatedWith()
    {
        using var action = CreateAction(settings: new() { ["osd"] = "osd-msg" });

        action.DialRotate(TestPayloads.Rotate(1));
        action.DialDown(null!);

        _mpv.Sent
            .Select(batch => batch.Single().Osd)
            .ShouldBe([MpvOsdMode.OsdMsg, MpvOsdMode.OsdMsg]);
    }

    [Fact]
    public void ReceivedSettings_UpdatesTheOsdSettingAndTellsTheAction()
    {
        using var action = CreateAction(settings: new() { ["osd"] = "osd-msg" });
        var newSettings = new JObject { ["osd"] = "no-osd" };

        action.ReceivedSettings(TestPayloads.Settings(newSettings));
        action.DialRotate(TestPayloads.Rotate(1));

        _mpv.Sent
            .Single()
            .Single().Osd
            .ShouldBe(MpvOsdMode.NoOsd);

        JToken.DeepEquals(action.SettingsReceived.ShouldHaveSingleItem(), newSettings).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ObservesThePropertiesUntilDisposed()
    {
        var action = CreateAction(MpvProperties.Volume, MpvProperties.Mute);

        _mpv.ObservedProperties.ShouldBe([MpvProperties.Volume, MpvProperties.Mute]);

        action.Dispose();

        _mpv.ObservedProperties.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithoutObservedProperties_ObservesNothingAndShowsNothing()
    {
        using var action = CreateAction();

        _mpv.ObservedProperties.ShouldBeEmpty();
        _connection.Feedback.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ShowsTheFeedbackForTheCurrentStateStraightAway()
    {
        using var action = CreateAction(MpvProperties.Volume);

        // The test action's feedback for any state is empty text and no progress.
        _connection.Feedback
            .ShouldHaveSingleItem()
            .ShouldBe(new() { ["indicator"] = "0", ["value"] = "" });
    }

    [Fact]
    public void StateChange_ShowsTheFeedbackOnTheDial()
    {
        using var action = CreateAction(MpvProperties.Volume);
        action.Feedback = state => new($"{state.Number(MpvProperties.Volume)}%", 42.5, "Images/icon");

        _mpv.Raise(TestStates.Connected((MpvProperties.Volume, 40d)));

        _connection.Feedback[^1].ShouldBe(new() { ["indicator"] = "42.5", ["value"] = "40%", ["icon"] = "Images/icon" });
    }

    [Fact]
    public void StateChange_WithoutAnIcon_LeavesTheIconOutOfTheFeedback()
    {
        using var action = CreateAction(MpvProperties.Volume);
        action.Feedback = _ => new("text", 10);

        _mpv.Raise(TestStates.Connected());

        _connection.Feedback[^1].ShouldNotContainKey("icon");
    }

    [Theory]
    [InlineData(-5, "0")]
    [InlineData(150, "100")]
    [InlineData(33.3333, "33.33")]
    public void StateChange_ClampsAndRoundsTheProgress(double progress, string expected)
    {
        using var action = CreateAction(MpvProperties.Volume);
        action.Feedback = _ => new("text", progress);

        _mpv.Raise(TestStates.Connected());

        _connection.Feedback[^1]["indicator"].ShouldBe(expected);
    }

    [Fact]
    public void StateChange_WhenTheFeedbackIsUnchanged_DoesNotUpdateTheDialAgain()
    {
        using var action = CreateAction(MpvProperties.Volume);
        action.Feedback = _ => new("text", 10);

        _mpv.Raise(TestStates.Connected(version: 1));
        var updates = _connection.Feedback.Count;
        _mpv.Raise(TestStates.Connected(version: 2));

        _connection.Feedback.Count.ShouldBe(updates);
    }

    [Fact]
    public void StateChange_AfterDispose_IsIgnored()
    {
        var action = CreateAction(MpvProperties.Volume);
        action.Feedback = _ => new("text", 10);
        action.Dispose();
        var updates = _connection.Feedback.Count;

        _mpv.Raise(TestStates.Connected());

        _connection.Feedback.Count.ShouldBe(updates);
    }

    [Fact]
    public void State_IsTheLatestStateForAnActionThatObservesProperties()
    {
        using var action = CreateAction(MpvProperties.Volume);
        var state = TestStates.Connected((MpvProperties.Volume, 40d));

        action.CurrentState.ShouldBeSameAs(MpvState.Inactive);

        _mpv.Raise(state);

        action.CurrentState.ShouldBeSameAs(state);
    }

    private TestDialAction CreateAction(params MpvProperty[] observedProperties) => CreateAction(null, observedProperties);

    private TestDialAction CreateAction(JObject? settings, params MpvProperty[] observedProperties)
        => new(_connection, TestPayloads.Initial(settings), _mpv, observedProperties);

    /// <summary>A dial action whose feedback the test controls. Rotating sends <c>rotate &lt;ticks&gt;</c> and pressing sends <c>press</c>.</summary>
    private sealed class TestDialAction(ISDConnection connection, InitialPayload payload, IMpvClient mpv, IReadOnlyCollection<MpvProperty> observedProperties)
        : MpvDialAction(connection, payload, mpv, observedProperties)
    {
        public Func<MpvState, DialFeedback> Feedback { get; set; } = _ => default;

        public List<JObject?> SettingsReceived { get; } = [];

        public MpvState CurrentState => State;

        protected override MpvCommand[] RotateCommands(int ticks) => [new("rotate", ticks)];

        protected override MpvCommand[] PressCommands => [new("press")];

        protected override DialFeedback GetFeedback(MpvState state) => Feedback(state);

        protected override void OnSettingsChanged(JObject? settings) => SettingsReceived.Add(settings);
    }
}
