using BarRaider.SdTools;
using System.Text.Json;

namespace MpvStreamDeckController
{
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
        /// Dictionary of observer properties id -> number of observers.
        /// </summary>
        private readonly Dictionary<int, int> _observedProperties = [];

        /// <summary>
        /// Dictionary of observer properties id -> last observed property value.
        /// </summary>
        private readonly Dictionary<int, object> _observedPropertyValues = [];

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
                    _observedProperties[property.Id] = _observedProperties.GetValueOrDefault(property.Id) + 1;
                }
            }

            _observedPropertiesChanged();
            return new PropertyHandle(() => UpdateReferenceCounts(properties));
        }

        public bool IsPropertyObserved(MpvProperty property)
        {
            lock (_stateLock)
            {
                return _observedProperties.ContainsKey(property.Id);
            }
        }

        private void UpdateReferenceCounts(IEnumerable<MpvProperty> properties)
        {
            lock (_stateLock)
            {
                foreach (var property in properties)
                {
                    if (--_observedProperties[property.Id] > 0)
                    {
                        continue;
                    }

                    _observedProperties.Remove(property.Id);
                    _observedPropertyValues.Remove(property.Id);
                }
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

        public void RegisterPropertyChange(int id, object? rawValue)
        {
            if (!MpvProperties.All.TryGetValue(id, out var property))
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
                if (value is null)
                {
                    if (!_observedPropertyValues.Remove(property.Id))
                    {
                        return;
                    }
                }
                else
                {
                    if (_observedPropertyValues.TryGetValue(property.Id, out var old) && Equals(old, value))
                    {
                        return;
                    }

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
            var next = new MpvState(_isConnected, new Dictionary<int, object>(_observedPropertyValues));
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
                    LogHelper.Log(TracingLevel.ERROR, $"mpv state handler failed: {ex.Message}");
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
}