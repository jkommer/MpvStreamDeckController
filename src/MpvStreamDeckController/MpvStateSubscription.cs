namespace MpvStreamDeckController;

public sealed class MpvStateSubscription<TView> : IDisposable where TView : struct, IEquatable<TView>
{
    private readonly Func<MpvState, TView> _getFeedback;
    private readonly Action<TView> _onFeedbackChanged;
    private readonly IDisposable _observedProperties;
    private readonly Lock _syncLock = new();
    private TView? _previousFeedback;
    private volatile bool _disposed;

    public MpvStateSubscription(
        IReadOnlyCollection<MpvProperty> observedProperties,
        Func<MpvState, TView> getFeedback,
        Action<TView> onFeedbackChanged)
    {
        _getFeedback = getFeedback;
        _onFeedbackChanged = onFeedbackChanged;

        _observedProperties = MpvClient.Instance.ObserveProperty(observedProperties);

        MpvClient.Instance.StateChanged += OnStateChanged;

        // Apply initial state straight as we do not know when the next state changed event might come in. 
        OnStateChanged(MpvClient.Instance.State);
    }

    private void OnStateChanged(MpvState state)
    {
        if (_disposed)
        {
            return;
        }

        var feedback = _getFeedback(state);

        lock (_syncLock)
        {
            if (_previousFeedback is { } previous && previous.Equals(feedback))
            {
                return;
            }

            _previousFeedback = feedback;
        }

        _onFeedbackChanged(feedback);
    }

    public void Dispose()
    {
        _disposed = true;
        MpvClient.Instance.StateChanged -= OnStateChanged;
        _observedProperties.Dispose();
    }        
}