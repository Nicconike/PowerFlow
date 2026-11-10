using PowerFlow.Core.Models;

namespace PowerFlow.Core.Providers;

/// <summary>
/// Abstraction for OEM and platform hardware profile control.
/// </summary>
public interface IHardwareProfileProvider
{
    /// <summary>
    /// Gets the detected hardware brand.
    /// </summary>
    HardwareBrand Brand { get; }

    /// <summary>
    /// Gets a value indicating whether hardware-level profile switching is supported.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Sets the hardware thermal or fan profile.
    /// </summary>
    /// <param name="profile">The target hardware profile.</param>
    /// <returns>True if applied successfully, otherwise false.</returns>
    bool SetProfile(HardwareProfile profile);

    /// <summary>
    /// Gets the current active hardware profile if available.
    /// </summary>
    HardwareProfile? GetCurrentProfile();
}
