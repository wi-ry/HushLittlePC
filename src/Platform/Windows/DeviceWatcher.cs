
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace HushLittlePC;

sealed class DeviceWatcher : IMMNotificationClient, IDisposable
{
    readonly MMDeviceEnumerator _enumerator = new();
    string? _lastId;
    DateTime _lastAt = DateTime.MinValue;

    public event Action<string>? DefaultOutputChanged;

    public DeviceWatcher() => _enumerator.RegisterEndpointNotificationCallback(this);

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        // Windows reports each role separately; Multimedia is the one that matters for normal playback.
        if (flow != DataFlow.Render || role != Role.Multimedia || string.IsNullOrEmpty(defaultDeviceId)) return;

        // Ignore duplicate notifications for the same device in quick succession.
        if (defaultDeviceId == _lastId && DateTime.UtcNow - _lastAt < TimeSpan.FromSeconds(2)) return;
        _lastId = defaultDeviceId;
        _lastAt = DateTime.UtcNow;

        DefaultOutputChanged?.Invoke(defaultDeviceId);
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    public void OnDeviceAdded(string pwstrDeviceId) { }
    public void OnDeviceRemoved(string deviceId) { }
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    public void Dispose()
    {
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
        _enumerator.Dispose();
    }
}
