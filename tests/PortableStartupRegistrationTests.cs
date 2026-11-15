using System.Runtime.Versioning;
using HushLittlePC;
using Microsoft.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
[SupportedOSPlatform("windows")]
public class PortableStartupRegistrationTests
{
    [TestMethod]
    public void Set_EnablesAndDisablesRegistrationInAnIsolatedRunKey()
    {
        var subKeyPath = $@"Software\HushLittlePC.Tests\{Guid.NewGuid():N}";
        try
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(subKeyPath, writable: true);
            Assert.IsNotNull(runKey);

            PortableStartupRegistration.Set(runKey, enable: true, @"C:\Program Files\Hush Little PC\HushLittlePC.exe");

            Assert.AreEqual(
                "\"C:\\Program Files\\Hush Little PC\\HushLittlePC.exe\" --background",
                runKey.GetValue("HushLittlePC"));
            Assert.AreEqual(RegistryValueKind.String, runKey.GetValueKind("HushLittlePC"));

            PortableStartupRegistration.Set(runKey, enable: false, processPath: null);

            Assert.IsNull(runKey.GetValue("HushLittlePC"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKeyPath, throwOnMissingSubKey: false);
        }
    }

    [TestMethod]
    public void Set_WhenExecutablePathIsUnavailable_RemovesExistingRegistration()
    {
        var subKeyPath = $@"Software\HushLittlePC.Tests\{Guid.NewGuid():N}";
        try
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(subKeyPath, writable: true);
            Assert.IsNotNull(runKey);
            runKey.SetValue("HushLittlePC", "old-value");

            PortableStartupRegistration.Set(runKey, enable: true, processPath: null);

            Assert.IsNull(runKey.GetValue("HushLittlePC"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKeyPath, throwOnMissingSubKey: false);
        }
    }
}
