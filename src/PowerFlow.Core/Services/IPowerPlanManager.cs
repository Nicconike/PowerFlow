namespace PowerFlow.Core.Services;

/// <summary>
/// Service for managing active Windows power schemes.
/// </summary>
public interface IPowerPlanManager
{
    /// <summary>
    /// Sets the active Windows power plan scheme.
    /// </summary>
    /// <param name="schemeGuid">The GUID of the target power plan.</param>
    /// <returns>True if applied successfully, otherwise false.</returns>
    bool SetActivePlan(Guid schemeGuid);

    /// <summary>
    /// Sets the active Windows power plan scheme by GUID string.
    /// </summary>
    bool SetActivePlan(string schemeGuidString);

    /// <summary>
    /// Retrieves the current active power plan scheme GUID.
    /// </summary>
    Guid? GetActivePlan();
}
