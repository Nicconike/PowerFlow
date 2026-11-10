namespace PowerFlow.Core.Services;

/// <summary>
/// Service to identify running game processes.
/// </summary>
public interface IProcessDetector
{
    /// <summary>
    /// Scans running processes and returns the name of an active game, or null.
    /// </summary>
    string? DetectActiveGameProcess();
}
