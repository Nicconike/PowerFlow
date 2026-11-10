using Microsoft.Extensions.Logging;
using PowerFlow.Core.Models;

namespace PowerFlow.Core.Providers.Desktop;

/// <summary>
/// Hardware provider for standard desktop computers and non-OEM laptops.
/// </summary>
public sealed class DesktopProvider(ILogger<DesktopProvider> logger) : IHardwareProfileProvider
{
    private HardwareProfile _currentProfile = HardwareProfile.Balance;

    public HardwareBrand Brand => HardwareBrand.GenericDesktop;

    public bool IsSupported => true;

    public bool SetProfile(HardwareProfile profile)
    {
        _currentProfile = profile;
        logger.LogDebug("Desktop hardware profile set to {Profile}", profile);
        return true;
    }

    public HardwareProfile? GetCurrentProfile() => _currentProfile;
}
