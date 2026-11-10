using System.Management;
using Microsoft.Extensions.Logging;
using PowerFlow.Core.Models;
using PowerFlow.Core.Providers.Desktop;
using PowerFlow.Core.Providers.Lenovo;

namespace PowerFlow.Core.Providers;

/// <summary>
/// Factory to detect host platform and instantiate the appropriate hardware provider.
/// </summary>
public sealed class HardwareProviderFactory(
    ILoggerFactory loggerFactory,
    ILogger<HardwareProviderFactory> logger)
{
    public IHardwareProfileProvider CreateProvider()
    {
        var brand = DetectHardwareBrand();
        logger.LogInformation("Detected hardware platform: {Brand}", brand);

        return brand switch
        {
            HardwareBrand.Lenovo => new LenovoLegionProvider(loggerFactory.CreateLogger<LenovoLegionProvider>()),
            _ => new DesktopProvider(loggerFactory.CreateLogger<DesktopProvider>())
        };
    }

    public HardwareBrand DetectHardwareBrand()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection.Cast<ManagementObject>())
            {
                using (obj)
                {
                    var manufacturer = obj["Manufacturer"]?.ToString()?.ToUpperInvariant() ?? string.Empty;

                    if (manufacturer.Contains("LENOVO", StringComparison.OrdinalIgnoreCase))
                    {
                        return HardwareBrand.Lenovo;
                    }

                    if (manufacturer.Contains("ASUS", StringComparison.OrdinalIgnoreCase))
                    {
                        return HardwareBrand.Asus;
                    }

                    if (manufacturer.Contains("MSI", StringComparison.OrdinalIgnoreCase) ||
                        manufacturer.Contains("MICRO-STAR", StringComparison.OrdinalIgnoreCase))
                    {
                        return HardwareBrand.Msi;
                    }

                    if (manufacturer.Contains("DELL", StringComparison.OrdinalIgnoreCase) ||
                        manufacturer.Contains("ALIENWARE", StringComparison.OrdinalIgnoreCase))
                    {
                        return HardwareBrand.Dell;
                    }

                    if (manufacturer.Contains("ACER", StringComparison.OrdinalIgnoreCase))
                    {
                        return HardwareBrand.Acer;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to query system manufacturer via WMI. Defaulting to GenericDesktop");
        }

        return HardwareBrand.GenericDesktop;
    }
}
