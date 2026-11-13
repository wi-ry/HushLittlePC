using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace HushLittlePC;

sealed class DeviceWatcher : IMMNotificationClient, IDisposable
{
    readonly MMDeviceEnumerator _enumerator = new();
    readonly DefaultOutputChangeFilter _filter = new();

    public event Action<string>? DefaultOutputChanged;

    public DeviceWatcher() => _enumerator.RegisterEndpointNotificationCallback(this);

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (!_filter.ShouldRaise(
            isRenderFlow: flow == DataFlow.Render,
            isMultimediaRole: role == Role.Multimedia,
            deviceId: defaultDeviceId,
            utcNow: DateTime.UtcNow))
            return;

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
