using System.Runtime.Versioning;
using Microsoft.Win32;

namespace HushLittlePC;

/// <summary>Applies portable startup registration to a caller-supplied Run key.</summary>
[SupportedOSPlatform("windows")]
static class PortableStartupRegistration
{
    const string ValueName = "HushLittlePC";

    public static void Set(RegistryKey runKey, bool enable, string? processPath)
    {
        if (enable && !string.IsNullOrWhiteSpace(processPath))
            runKey.SetValue(ValueName, $"\"{processPath}\" --background", RegistryValueKind.String);
        else
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
