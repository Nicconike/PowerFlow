using PowerFlow.Core.Models;

namespace PowerFlow.Core.Services;

/// <summary>
/// State machine coordinator that evaluates process states and applies power profiles.
/// </summary>
public interface IProfileCoordinator : IDisposable
{
    event Action<ProfileState, string?>? StateChanged;

    ProfileState CurrentState { get; }

    string? CurrentGameProcess { get; }

    bool IsPaused { get; }

    void Start();

    void Stop();

    void Pause();

    void Resume();

    void ForceGamingMode();

    void ForceDesktopMode();

    void Evaluate();
}
