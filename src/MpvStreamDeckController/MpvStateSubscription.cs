namespace MpvStreamDeckController;

public sealed class MpvStateSubscription<TView> : IDisposable where TView : struct, IEquatable<TView>
{
    private readonly IMpvStateSource _source;
    private readonly Func<MpvState, TView> _getFeedback;
    private readonly Action<TView> _onFeedbackChanged;
    private readonly IDisposable _observedProperties;
    private readonly Lock _syncLock = new();
    private TView? _previousFeedback;
    private long _lastVersion = -1;
    private volatile bool _disposed;

    public MpvStateSubscription(IReadOnlyCollection<MpvProperty> observedProperties, Func<MpvState, TView> getFeedback, Action<TView> onFeedbackChanged)
        : this(MpvClient.Instance, observedProperties, getFeedback, onFeedbackChanged)
    {
    }

    internal MpvStateSubscription(IMpvStateSource source, IReadOnlyCollection<MpvProperty> observedProperties, Func<MpvState, TView> getFeedback, Action<TView> onFeedbackChanged)
    {
        _source = source;
        _getFeedback = getFeedback;
        _onFeedbackChanged = onFeedbackChanged;

        _observedProperties = source.ObserveProperty(observedProperties);

        source.StateChanged += OnStateChanged;

        // Apply the initial state straight away, as we don't know when the next state change will come in. If a newer state arrives between reading State and applying it, the version check below discards this one.
        OnStateChanged(source.State);
    }

    private void OnStateChanged(MpvState state)
    {
        if (_disposed)
        {
            return;
        }

        // States raised on different threads can arrive out of order. Everything happens under the lock, so feedback is computed and handed on strictly in version order.
        lock (_syncLock)
        {
            if (state.Version <= _lastVersion)
            {
                return;
            }

            _lastVersion = state.Version;

            var feedback = _getFeedback(state);

            if (_previousFeedback is { } previous && previous.Equals(feedback))
            {
                return;
            }

            _previousFeedback = feedback;
            // Callbacks only start an async update and return, so holding the lock here is cheap.
            _onFeedbackChanged(feedback);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _source.StateChanged -= OnStateChanged;
        _observedProperties.Dispose();
    }
}
