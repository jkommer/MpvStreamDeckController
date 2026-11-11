using BarRaider.SdTools;
using System.Text.Json;

namespace MpvStreamDeckController;

/// <summary>
/// Helper to track the state of mpv and any observed properties.
/// 
/// Keeps track of property values producing a new immuntable <see cref="MpvState"/> when changes have occurred and raises <see cref="StateChanged"/>.
/// </summary>
/// <param name="observedPropertiesChanged">Called whenever the set of observed properties may have changed. Used to communicate the (un-)observed properties to mpv</param>
public sealed class MpvStateTracker(Action observedPropertiesChanged)
{
    private readonly Action _observedPropertiesChanged = observedPropertiesChanged;

    private readonly Lock _stateLock = new();

    /// <summary>
    /// Dictionary of observed property id -> number of observers.
    /// </summary>
    private readonly Dictionary<long, int> _observedProperties = [];

    /// <summary>
    /// Dictionary of observed property id -> last observed property value.
    /// </summary>
    private readonly Dictionary<long, object> _observedPropertyValues = [];

    /// <summary>
    /// Dictionary of observed property id -> how many times the property has gone from unobserved to observed.
    /// </summary>
    private readonly Dictionary<long, int> _observationGenerations = [];

    private bool _isConnected;
    private MpvState _state = MpvState.Inactive;
    public MpvState State => Volatile.Read(ref _state);
    public event Action<MpvState>? StateChanged;

    /// <summary>
    /// Registers a property to be observed. Dispose the returned <see cref="IDisposable" /> handle to indicate that this 
    /// registration is no longer needed.
    /// 
    /// Properties will automatically be unobserved when the number of interested observers reaches 0.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    public IDisposable ObserveProperty(IEnumerable<MpvProperty> properties)
    {
        lock (_stateLock)
        {
            foreach (var property in properties)
            {
                var count = _observedProperties.GetValueOrDefault(property.Id);

                if (count == 0)
                {
                    _observationGenerations[property.Id] = _observationGenerations.GetValueOrDefault(property.Id) + 1;
                }

                _observedProperties[property.Id] = count + 1;
            }
        }

        _observedPropertiesChanged();
        return new PropertyHandle(() => UpdateReferenceCounts(properties));
    }

    /// <summary>
    /// Null if the property is not observed. Otherwise a number that changes each time the property goes from
    /// unobserved to observed. If it differs from the one a property was registered with in mpv, the property was
    /// dropped and re-added in between: its value was discarded, and mpv only sends a value when it changes, so it
    /// must be registered again to get the current value.
    /// </summary>
    public int? GetObservationGeneration(MpvProperty property)
    {
        lock (_stateLock)
        {
            return _observedProperties.ContainsKey(property.Id) ? _observationGenerations[property.Id] : null;
        }
    }

    private void UpdateReferenceCounts(IEnumerable<MpvProperty> properties)
    {
        MpvState? snapshot = null;

        lock (_stateLock)
        {
            var valuesRemoved = false;

            foreach (var property in properties)
            {
                if (--_observedProperties[property.Id] > 0)
                {
                    continue;
                }

                _observedProperties.Remove(property.Id);
                valuesRemoved |= _observedPropertyValues.Remove(property.Id);
            }

            // Publish the removal, so State doesn't keep offering a value nobody is tracking any more.
            if (valuesRemoved)
            {
                snapshot = CreateSnapshot();
            }
        }

        if (snapshot is not null)
        {
            RaiseStateChange(snapshot);
        }

        _observedPropertiesChanged();
    }

    /// <summary>
    /// Registers that the connection to mpv has been successfully established.
    /// </summary>
    public void Connected()
    {
        MpvState snapshot;

        lock (_stateLock)
        {
            if (_isConnected)
            {
                return;
            }

            _isConnected = true;
            snapshot = CreateSnapshot();
        }

        RaiseStateChange(snapshot);
    }

    /// <summary>
    /// Registers that we have disconnected from mpv and initiate a state reset. 
    /// </summary>
    public void Disconnected()
    {
        MpvState snapshot;

        lock (_stateLock)
        {
            if (!_isConnected && _observedPropertyValues.Count == 0)
            {
                return;
            }

            _isConnected = false;
            _observedPropertyValues.Clear();
            snapshot = CreateSnapshot();
        }

        RaiseStateChange(snapshot);
    }

    public void RegisterPropertyChange(long propertyId, object? rawValue)
    {
        if (!MpvProperties.All.TryGetValue(propertyId, out var property))
        {
            return;
        }

        object? value = rawValue;

        if (rawValue is double doubleValue && property.Quantize is { } quantize)
        {
            value = quantize(doubleValue);
        }
        else if (rawValue is JsonElement json)
        {
            value = property.FromJson?.Invoke(json);
        }

        MpvState snapshot;

        lock (_stateLock)
        {

            if (!_observedProperties.ContainsKey(property.Id))
            {
                // late event for a property that is no longer observed.
                return;
            }

            // Verify the value has changed before we inform the observers. 
            _observedPropertyValues.TryGetValue(property.Id, out var old);

            if (Equals(old, value))
            {
                return;
            }

            if (value is null)
            {
                _observedPropertyValues.Remove(property.Id);
            }
            else
            {
                _observedPropertyValues[property.Id] = value;
            }

            snapshot = CreateSnapshot();
        }

        RaiseStateChange(snapshot);
    }

    /// <summary>
    /// Create a snapshot of the observed property values.
    /// 
    /// Must be called from a lock(_stateLock) {}
    /// </summary>
    /// <returns></returns>
    private MpvState CreateSnapshot()
    {
        var next = new MpvState(_state.Version + 1, _isConnected, new Dictionary<long, object>(_observedPropertyValues));
        Volatile.Write(ref _state, next);
        return next;
    }

    private void RaiseStateChange(MpvState state)
    {
        if (StateChanged is not { } handlers)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Action<MpvState>>())
        {
            try
            {
                handler(state);
            }
            catch (Exception ex)
            {
                LogHelper.Log(TracingLevel.ERROR, $"mpv state handler failed: {ex}");
            }
        }
    }

    private sealed class PropertyHandle(Action release) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                release();
            }
        }
    }
}