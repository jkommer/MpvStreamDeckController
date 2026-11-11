using System.Text.Json;
using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvStateTrackerTests
{
    [Fact]
    public void ConnectionToMpv_WhenStatusChanged_RaisesStateChange()
    {
        // Bundling Connected and Disconnected to avoid repetition.

        var invocations = 0;
        var isConnected = false;

        var stateTracker = new MpvStateTracker(() => { });

        stateTracker.StateChanged += state =>
        {
            invocations++;
            isConnected = state.IsConnected;
        };

        // initial state: disconnected -> disconnected - no-op.
        stateTracker.Disconnected();
        invocations.ShouldBe(0);
        isConnected.ShouldBeFalse();

        // disconnected -> connected - raise. 
        stateTracker.Connected();
        invocations.ShouldBe(1);
        isConnected.ShouldBeTrue();

        // connected -> connected - no-op. 
        stateTracker.Connected();
        invocations.ShouldBe(1);
        isConnected.ShouldBeTrue();

        // connected -> disconnected -> raise. 
        stateTracker.Disconnected();
        invocations.ShouldBe(2);
        isConnected.ShouldBeFalse();
    }

    [Fact]
    public void GetObservationGeneration_DefaultState_TracksNothing()
    {
        var stateTracker = new MpvStateTracker(() => { });
        VerifyPropertyObservationState(stateTracker, [], MpvProperties.All.Values);
    }

    public static TheoryData<IEnumerable<MpvProperty>> GetObservationGeneration_WhenQueryingATrackedProperty_ShouldNotBeNullData => new()
    {
        { [MpvProperties.ACompressor] },
        { [MpvProperties.TimePos, MpvProperties.Duration] },
        { [MpvProperties.Mute, MpvProperties.Volume, MpvProperties.ACompressor] },
        MpvProperties.All.Values,
    };

    [Theory]
    [MemberData(nameof(GetObservationGeneration_WhenQueryingATrackedProperty_ShouldNotBeNullData))]
    public void GetObservationGeneration_WhenQueryingATrackedProperty_ShouldNotBeNull(IEnumerable<MpvProperty> observedProperties)
    {
        var invocations = 0;
        var stateTracker = new MpvStateTracker(() => { invocations++; });

        using var _ = stateTracker.ObserveProperty(observedProperties);

        VerifyPropertyObservationState(stateTracker, observedProperties, MpvProperties.All.Values.Except(observedProperties));
        invocations.ShouldBe(1);
    }

    [Fact]
    public void GetObservationGeneration_WhenAPropertyHasMultipleObservers_ShouldNotifyForEachRequest()
    {
        var invocations = 0;
        var stateTracker = new MpvStateTracker(() => { invocations++; });

        // register two "distinct" observers. 
        using var _ = stateTracker.ObserveProperty([MpvProperties.TimePos]);
        using var _1 = stateTracker.ObserveProperty([MpvProperties.TimePos]);

        VerifyPropertyObservationState(stateTracker, [MpvProperties.TimePos], MpvProperties.All.Values.Except([MpvProperties.TimePos]));

        // I might change my mind about this in the future; but currently I want this to reflect the number of calls for properties to be observed; rather than distinct property observations. 
        invocations.ShouldBe(2);
    }

    [Fact]
    public void GetObservationGeneration_WhenAPropertyHasMultipleObserversAndOneUnobserves_ShouldNotBeNull()
    {
        var invocations = 0;
        var stateTracker = new MpvStateTracker(() => { invocations++; });

        // register two "distinct" observers. 
        var observedProperty = stateTracker.ObserveProperty([MpvProperties.TimePos]);
        using var _ = stateTracker.ObserveProperty([MpvProperties.TimePos]);

        VerifyPropertyObservationState(stateTracker, [MpvProperties.TimePos], MpvProperties.All.Values.Except([MpvProperties.TimePos]));

        observedProperty.Dispose(); // explicit disposal so we can check the disposal triggers unobserve correctly. 
        invocations.ShouldBe(3); // observe, observe, unobserve.

        VerifyPropertyObservationState(stateTracker, [MpvProperties.TimePos], MpvProperties.All.Values.Except([MpvProperties.TimePos]));
    }

    [Fact]
    public void GetObservationGeneration_WhenAllPropertyObserversUnsubscribed_ShouldBeNull()
    {
        var invocations = 0;
        var stateTracker = new MpvStateTracker(() => { invocations++; });

        // register two "distinct" observers. 
        var observedProperty1 = stateTracker.ObserveProperty([MpvProperties.TimePos]);
        var observedProperty2 = stateTracker.ObserveProperty([MpvProperties.TimePos]);

        VerifyPropertyObservationState(stateTracker, [MpvProperties.TimePos], MpvProperties.All.Values.Except([MpvProperties.TimePos]));

        observedProperty1.Dispose();
        observedProperty2.Dispose();
        invocations.ShouldBe(4); // observe, observe, unobserve, unobserve 

        VerifyPropertyObservationState(stateTracker, [], MpvProperties.All.Values);
    }

    [Fact]
    public void StateChanged_EachPublishedState_HasAHigherVersionThanTheLast()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var versions = new List<long>();
        stateTracker.StateChanged += state => versions.Add(state.Version);

        var handle = stateTracker.ObserveProperty([MpvProperties.Volume]);
        stateTracker.Connected();
        stateTracker.RegisterPropertyChange(MpvProperties.Volume.Id, 10d);
        stateTracker.RegisterPropertyChange(MpvProperties.Volume.Id, 20d);
        handle.Dispose();
        stateTracker.Disconnected();

        versions.Count.ShouldBe(5); // connected, 10, 20, value removed, disconnected
        versions[0].ShouldBeGreaterThan(MpvState.Inactive.Version);

        versions
            .Zip(versions.Skip(1))
            .ShouldAllBe(pair => pair.Second > pair.First);

        stateTracker.State.Version.ShouldBe(versions[^1]);
    }

    [Fact]
    public void GetObservationGeneration_ChangesOnlyWhenAPropertyIsDroppedAndReAdded()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var pause = MpvProperties.Pause;

        stateTracker.GetObservationGeneration(pause).ShouldBeNull();

        var first = stateTracker.ObserveProperty([pause]);
        var initial = stateTracker.GetObservationGeneration(pause);
        initial.ShouldNotBeNull();

        // Another observer joining or leaving while the property stays observed doesn't change it.
        var second = stateTracker.ObserveProperty([pause]);
        second.Dispose();

        stateTracker
            .GetObservationGeneration(pause)
            .ShouldBe(initial);

        first.Dispose();

        stateTracker
            .GetObservationGeneration(pause)
            .ShouldBeNull();

        using var _ = stateTracker.ObserveProperty([pause]);

        stateTracker
            .GetObservationGeneration(pause)
            .ShouldNotBe(initial);
    }

    [Fact]
    public void ReleasingTheLastObserver_WhenThePropertyHasAValue_PublishesAStateWithoutIt()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var handle = stateTracker.ObserveProperty([MpvProperties.Volume]);
        stateTracker.RegisterPropertyChange(MpvProperties.Volume.Id, 40d);

        MpvState? published = null;
        stateTracker.StateChanged += state => published = state;

        handle.Dispose();

        published
            .ShouldNotBeNull()
            .Number(MpvProperties.Volume)
            .ShouldBeNull();

        stateTracker.State
            .Number(MpvProperties.Volume)
            .ShouldBeNull();
    }

    [Fact]
    public void ReleasingAnObserver_WhenNoValueIsRemoved_DoesNotPublishAState()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var withValue = stateTracker.ObserveProperty([MpvProperties.Volume]);
        using var stillObserving = stateTracker.ObserveProperty([MpvProperties.Volume]);
        var withoutValue = stateTracker.ObserveProperty([MpvProperties.Mute]);
        stateTracker.RegisterPropertyChange(MpvProperties.Volume.Id, 40d);

        var invocations = 0;
        stateTracker.StateChanged += _ => invocations++;

        withValue.Dispose();    // volume is still observed by another handle, so its value stays
        withoutValue.Dispose(); // mute never had a value

        invocations.ShouldBe(0);

        stateTracker.State
            .Number(MpvProperties.Volume)
            .ShouldBe(40);
    }

    private static void VerifyPropertyObservationState(MpvStateTracker stateTracker, IEnumerable<MpvProperty> trackedProperties, IEnumerable<MpvProperty> untrackedProperties)
    {
        foreach (var property in trackedProperties)
        {
            stateTracker
                .GetObservationGeneration(property)
                .ShouldNotBeNull($"property: {property.Name}");
        }

        foreach (var property in untrackedProperties)
        {
            stateTracker
                .GetObservationGeneration(property)
                .ShouldBeNull($"property: {property.Name}");
        }
    }

    [Fact]
    public void RegisterPropertyChange_StateChangeForUnobservedProperty_DoesNotRaiseStateChange()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var invocations = 0;

        stateTracker.StateChanged += state =>
        {
            invocations++;
        };

        stateTracker.RegisterPropertyChange(MpvProperties.Duration.Id, "3:00");
        invocations.ShouldBe(0);
    }

    [Fact]
    public void RegisterPropertyChange_StateChangeForObservedProperty_RaisesStateChange()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var invocations = 0;

        var expectedDuration = 180d;
        var nextExpectedDuration = 360d;

        double? receivedDuration = null;

        stateTracker.StateChanged += state =>
        {
            invocations++;
            receivedDuration = state.Number(MpvProperties.Duration);
        };

        // We do not care about being connected for state changes; as this might be a state reset or similar and we want buttons/dials/icons to correctly reflect what we're told. 
        stateTracker.ObserveProperty([MpvProperties.Duration]);
        stateTracker.RegisterPropertyChange(MpvProperties.Duration.Id, expectedDuration);

        invocations.ShouldBe(1);
        receivedDuration.ShouldBe(expectedDuration);

        stateTracker.State
            .Number(MpvProperties.Duration)
            .ShouldBe(expectedDuration);

        stateTracker.RegisterPropertyChange(MpvProperties.Duration.Id, nextExpectedDuration);

        // Verify state updates on subsequent calls.
        invocations.ShouldBe(2);
        receivedDuration.ShouldBe(nextExpectedDuration);

        stateTracker.State
            .Number(MpvProperties.Duration)
            .ShouldBe(nextExpectedDuration);
    }

    [Fact]
    public void RegisterPropertyChange_DuplicateStateChangeForObservedProperty_DoesNotRaiseStateChange()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var invocations = 0;

        var expectedDuration = 180d;

        double? receivedDuration = null;

        stateTracker.StateChanged += state =>
        {
            invocations++;
            receivedDuration = state.Number(MpvProperties.Duration);
        };

        stateTracker.ObserveProperty([MpvProperties.Duration]);
        stateTracker.RegisterPropertyChange(MpvProperties.Duration.Id, expectedDuration);

        invocations.ShouldBe(1);
        receivedDuration.ShouldBe(expectedDuration);

        stateTracker.State
            .Number(MpvProperties.Duration)
            .ShouldBe(expectedDuration);

        // Nothing should be raised when we get notified for the exact same property + state. 
        stateTracker.RegisterPropertyChange(MpvProperties.Duration.Id, expectedDuration);

        invocations.ShouldBe(1);
        receivedDuration.ShouldBe(expectedDuration);

        stateTracker.State
            .Number(MpvProperties.Duration)
            .ShouldBe(expectedDuration);
    }

    [Fact]
    public void RegisterPropertyChange_UnknownPropertyChanged_DoesNotRaiseStateChange()
    {
        var stateTracker = new MpvStateTracker(() => { });
        var invocations = 0;

        stateTracker.StateChanged += state =>
        {
            invocations++;
        };

        stateTracker.ObserveProperty(MpvProperties.All.Values);
        stateTracker.RegisterPropertyChange(MpvProperties.All.Keys.Max() + 1, "lipsum");

        invocations.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RegisterPropertyChange_WhenJsonPropertyWithFromJsonSet_RaisesEventAndProvidesCorrectValue(bool stateValue)
    {
        // ACompressor is awkward - make sure we don't break it's response. 
        var response = JsonElement.Parse($$"""[{ "name":"acompressor","label":"acompressor","enabled":{{stateValue.ToString().ToLower()}},"params":{ "makeup":"8dB","attack":"20","release":"250","threshold":"-25dB","ratio":"3","knee":"2dB"} }]""");
        bool? stateResult = null;

        var stateTracker = new MpvStateTracker(() => { });
        var invocations = 0;

        stateTracker.StateChanged += state =>
        {
            invocations++;
            stateResult = state.Flag(MpvProperties.ACompressor);

        };

        stateTracker.ObserveProperty([MpvProperties.ACompressor]);
        stateTracker.RegisterPropertyChange(MpvProperties.ACompressor.Id, response);

        invocations.ShouldBe(1);
        stateResult.ShouldBe(stateValue);
    }
}
