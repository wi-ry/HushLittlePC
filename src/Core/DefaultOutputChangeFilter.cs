namespace HushLittlePC;

/// <summary>Filters default-output notifications to the render/multimedia endpoint and suppresses duplicates.</summary>
sealed class DefaultOutputChangeFilter
{
    static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(2);
    string? _lastDeviceId;
    DateTime _lastAt = DateTime.MinValue;

    public bool ShouldRaise(bool isRenderFlow, bool isMultimediaRole, string? deviceId, DateTime utcNow)
    {
        if (!isRenderFlow || !isMultimediaRole || string.IsNullOrEmpty(deviceId))
            return false;

        if (deviceId == _lastDeviceId && utcNow - _lastAt < DuplicateWindow)
            return false;

        _lastDeviceId = deviceId;
        _lastAt = utcNow;
        return true;
    }
}
