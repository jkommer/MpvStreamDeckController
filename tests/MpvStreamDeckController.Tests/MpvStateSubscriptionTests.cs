using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvStateSubscriptionTests
{
    private static readonly MpvProperty Volume = MpvProperties.Volume;

    [Fact]
    public void Constructor_AppliesTheCurrentState()
    {
        var source = new FakeStateSource { State = CreateVolumeState(1, 10) };
        var feedback = new List<double?>();

        using var _ = Subscribe(source, feedback);

        feedback.ShouldBe([10]);
        source.ObservedProperties.ShouldBe([Volume]);
    }

    [Fact]
    public void Constructor_WhenANewerStateArrivesBetweenReadingAndApplyingTheCurrentOne_KeepsTheNewerState()
    {
        var source = new FakeStateSource();
        var feedback = new List<double?>();

        // The constructor reads State and gets v1, but v2 is raised before it applies v1.
        source.OnStateRead = () => source.Raise(CreateVolumeState(2, 20));
        source.State = CreateVolumeState(1, 10);

        using var _ = Subscribe(source, feedback);

        feedback.ShouldBe([20]);
    }

    [Fact]
    public void StateChanged_WhenAnOlderStateArrivesLate_IgnoresIt()
    {
        var source = new FakeStateSource { State = CreateVolumeState(1, 10) };
        var feedback = new List<double?>();
        using var _ = Subscribe(source, feedback);

        source.Raise(CreateVolumeState(3, 30));
        source.Raise(CreateVolumeState(2, 20));

        feedback.ShouldBe([10, 30]);
    }

    [Fact]
    public void StateChanged_WithTheSameVersionAgain_DoesNothing()
    {
        var source = new FakeStateSource { State = CreateVolumeState(1, 10) };
        var feedback = new List<double?>();
        using var _ = Subscribe(source, feedback);

        // Same version, different content: can't happen in practice, but shows the version alone decides.
        source.Raise(CreateVolumeState(1, 99));

        feedback.ShouldBe([10]);
    }

    [Fact]
    public void StateChanged_WhenTheFeedbackIsUnchanged_DoesNotCallBack()
    {
        var source = new FakeStateSource { State = CreateVolumeState(1, 10) };
        var feedback = new List<double?>();
        using var _ = Subscribe(source, feedback);

        source.Raise(CreateVolumeState(2, 10));
        source.Raise(CreateVolumeState(3, 15));

        feedback.ShouldBe([10, 15]);
    }

    [Fact]
    public void Dispose_StopsDeliveringAndReleasesTheObservedProperties()
    {
        var source = new FakeStateSource { State = CreateVolumeState(1, 10) };
        var feedback = new List<double?>();
        var subscription = Subscribe(source, feedback);

        subscription.Dispose();
        source.Raise(CreateVolumeState(2, 20));

        feedback.ShouldBe([10]);
        source.ObservedProperties.ShouldBeEmpty();
    }

    private static MpvStateSubscription<Feedback> Subscribe(FakeStateSource source, List<double?> feedback)
        => new(source, [Volume], state => new Feedback(state.Number(Volume)), f => feedback.Add(f.Volume));

    private static MpvState CreateVolumeState(long version, double volume)
        => new(version, true, new Dictionary<long, object> { [Volume.Id] = volume });

    private readonly record struct Feedback(double? Volume);

    private sealed class FakeStateSource : IMpvStateSource
    {
        private MpvState _state = MpvState.Inactive;

        public List<MpvProperty> ObservedProperties { get; } = [];

        /// <summary>Runs (once) when State is read, to simulate a state change landing straight after the read.</summary>
        public Action? OnStateRead { get; set; }

        public MpvState State
        {
            get
            {
                var state = _state;
                var onRead = OnStateRead;
                OnStateRead = null;
                onRead?.Invoke();
                return state;
            }
            set => _state = value;
        }

        public event Action<MpvState>? StateChanged;

        public void Raise(MpvState state)
        {
            _state = state;
            StateChanged?.Invoke(state);
        }

        public IDisposable ObserveProperty(IReadOnlyCollection<MpvProperty> properties)
        {
            ObservedProperties.AddRange(properties);
            return new Release(() => ObservedProperties.RemoveAll(properties.Contains));
        }

        private sealed class Release(Action release) : IDisposable
        {
            public void Dispose() => release();
        }
    }
}
