using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerFlow.Core.Configuration;

namespace PowerFlow.Core.Services;

/// <summary>
/// Scans running processes to detect games with minimal memory and CPU overhead.
/// </summary>
public sealed class ProcessDetector(
    IOptions<PowerFlowOptions> options,
    ILogger<ProcessDetector> logger) : IProcessDetector
{
    public string? DetectActiveGameProcess()
    {
        var config = options.Value;
        var ignored = new HashSet<string>(config.IgnoredProcesses, StringComparer.OrdinalIgnoreCase);
        var customNames = new HashSet<string>(config.CustomProcessNames, StringComparer.OrdinalIgnoreCase);

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enumerate running processes");
            return null;
        }

        try
        {
            foreach (var process in processes)
            {
                var name = process.ProcessName;

                if (ignored.Contains(name))
                {
                    continue;
                }

                if (customNames.Contains(name))
                {
                    logger.LogDebug("Detected matching custom game process: {Name}", name);
                    return name;
                }

                if (config.GameDirectoryKeywords.Count > 0)
                {
                    try
                    {
                        var filePath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            foreach (var keyword in config.GameDirectoryKeywords)
                            {
                                if (filePath.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                                {
                                    logger.LogDebug("Detected game by path keyword '{Keyword}': {Path}", keyword, filePath);
                                    return name;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Access denied on system/protected processes is expected
                    }
                }
            }
        }
        finally
        {
            foreach (var p in processes)
            {
                p.Dispose();
            }
        }

        return null;
    }
}
