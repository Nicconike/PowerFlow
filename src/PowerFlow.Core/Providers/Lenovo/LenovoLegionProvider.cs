using System.Management;
using Microsoft.Extensions.Logging;
using PowerFlow.Core.Models;

namespace PowerFlow.Core.Providers.Lenovo;

/// <summary>
/// Hardware provider for Lenovo Legion laptops using WMI ACPI methods.
/// </summary>
public sealed class LenovoLegionProvider(ILogger<LenovoLegionProvider> logger) : IHardwareProfileProvider
{
    private const string WmiNamespace = @"root\wmi";
    private const string WmiClassName = "LENOVO_GAMEZONE_DATA";

    public HardwareBrand Brand => HardwareBrand.Lenovo;

    public bool IsSupported => CheckSupport();

    public bool SetProfile(HardwareProfile profile)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection.Cast<ManagementObject>())
            {
                using (obj)
                {
                    using var inParams = obj.GetMethodParameters("SetSmartFanMode");
                    inParams["Data"] = (uint)profile;
                    using var outParams = obj.InvokeMethod("SetSmartFanMode", inParams, null);

                    logger.LogInformation("Applied Lenovo Legion thermal mode {Profile}", profile);
                    return true;
                }
            }

            logger.LogWarning("No LENOVO_GAMEZONE_DATA instance found to set thermal mode");
            return false;
        }
        catch (ManagementException ex)
        {
            logger.LogWarning(ex, "Failed to set Lenovo thermal mode via WMI. Ensure application runs with elevated privileges");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error setting Lenovo thermal mode");
            return false;
        }
    }

    public HardwareProfile? GetCurrentProfile()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection.Cast<ManagementObject>())
            {
                using (obj)
                {
                    using var outParams = obj.InvokeMethod("GetSmartFanMode", null, null);
                    if (outParams?["Data"] is uint modeVal && Enum.IsDefined(typeof(HardwareProfile), (int)modeVal))
                    {
                        return (HardwareProfile)(int)modeVal;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not query current Lenovo thermal mode");
        }

        return null;
    }

    private bool CheckSupport()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            using var collection = searcher.Get();
            return collection.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}
