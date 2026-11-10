using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace PowerFlow.Core.Services;

/// <summary>
/// Direct Win32 implementation of Windows power plan management via powrprof.dll.
/// </summary>
public sealed partial class WindowsPowerManager(ILogger<WindowsPowerManager> logger) : IPowerPlanManager
{
    private const uint ErrorSuccess = 0;

    [LibraryImport("powrprof.dll")]
    private static partial uint PowerSetActiveScheme(IntPtr userRootPowerKey, in Guid schemeGuid);

    [LibraryImport("powrprof.dll")]
    private static partial uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr LocalFree(IntPtr hMem);

    public bool SetActivePlan(Guid schemeGuid)
    {
        try
        {
            var result = PowerSetActiveScheme(IntPtr.Zero, in schemeGuid);
            if (result == ErrorSuccess)
            {
                logger.LogInformation("Switched Windows power plan to {Guid}", schemeGuid);
                return true;
            }

            logger.LogError("PowerSetActiveScheme failed with error code {Code}", result);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while calling PowerSetActiveScheme");
            return false;
        }
    }

    public bool SetActivePlan(string schemeGuidString)
    {
        if (Guid.TryParse(schemeGuidString, out var guid))
        {
            return SetActivePlan(guid);
        }

        logger.LogError("Invalid GUID format for power plan: {Input}", schemeGuidString);
        return false;
    }

    public Guid? GetActivePlan()
    {
        var result = PowerGetActiveScheme(IntPtr.Zero, out var pGuid);
        if (result != ErrorSuccess || pGuid == IntPtr.Zero)
        {
            logger.LogWarning("PowerGetActiveScheme failed with error code {Code}", result);
            return null;
        }

        try
        {
            return Marshal.PtrToStructure<Guid>(pGuid);
        }
        finally
        {
            LocalFree(pGuid);
        }
    }
}
