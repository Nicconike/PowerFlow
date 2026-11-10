namespace PowerFlow.Core.Models;

/// <summary>
/// Represents the operating state of the power profile coordinator.
/// </summary>
public enum ProfileState
{
    /// <summary>
    /// Desktop mode with balanced power plan and normal cooling.
    /// </summary>
    DesktopBalanced = 0,

    /// <summary>
    /// Gaming mode with high-performance power plan and aggressive cooling.
    /// </summary>
    GamingHighPerformance = 1,

    /// <summary>
    /// Automatic switching paused by user override.
    /// </summary>
    Paused = 2
}
