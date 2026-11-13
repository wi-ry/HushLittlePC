namespace HushLittlePC;

/// <summary>Validates notification activation arguments before an Unmute action is dispatched.</summary>
static class ToastActivationPolicy
{
    public static bool TryGetUnmuteDeviceId(string? action, string? deviceId, out string targetDeviceId)
    {
        targetDeviceId = string.Empty;
        if (action != "unmute" || string.IsNullOrWhiteSpace(deviceId))
            return false;

        targetDeviceId = deviceId;
        return true;
    }
}
