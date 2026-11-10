using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerFlow.Core.Configuration;
using PowerFlow.Core.Models;
using PowerFlow.Core.Providers;

namespace PowerFlow.Core.Services;

/// <summary>
/// State machine coordinating Windows power plans and OEM hardware profiles.
/// </summary>
public sealed class ProfileCoordinator : IProfileCoordinator
{
    private readonly IProcessDetector _detector;
    private readonly IPowerPlanManager _powerManager;
    private readonly IHardwareProfileProvider _hardwareProvider;
    private readonly IOptions<PowerFlowOptions> _options;
    private readonly ILogger<ProfileCoordinator> _logger;

    private Timer? _timer;
    private DateTime? _gracePeriodDeadline;
    private readonly object _lock = new();

    public event Action<ProfileState, string?>? StateChanged;

    public ProfileState CurrentState { get; private set; } = ProfileState.DesktopBalanced;

    public string? CurrentGameProcess { get; private set; }

    public bool IsPaused { get; private set; }

    public ProfileCoordinator(
        IProcessDetector detector,
        IPowerPlanManager powerManager,
        IHardwareProfileProvider hardwareProvider,
        IOptions<PowerFlowOptions> options,
        ILogger<ProfileCoordinator> logger)
    {
        _detector = detector;
        _powerManager = powerManager;
        _hardwareProvider = hardwareProvider;
        _options = options;
        _logger = logger;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_timer != null)
            {
                return;
            }

            var intervalMs = Math.Max(1000, _options.Value.PollIntervalSeconds * 1000);
            _timer = new Timer(_ => Evaluate(), null, 0, intervalMs);
            _logger.LogInformation("ProfileCoordinator started with polling interval {Interval} ms", intervalMs);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
            _logger.LogInformation("ProfileCoordinator stopped");
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            IsPaused = true;
            CurrentState = ProfileState.Paused;
            _gracePeriodDeadline = null;
            _logger.LogInformation("ProfileCoordinator paused by user");
            StateChanged?.Invoke(CurrentState, CurrentGameProcess);
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            IsPaused = false;
            _gracePeriodDeadline = null;
            _logger.LogInformation("ProfileCoordinator resumed by user");
            Evaluate();
        }
    }

    public void ForceGamingMode()
    {
        lock (_lock)
        {
            IsPaused = true;
            ApplyGamingProfile("Manual Override");
        }
    }

    public void ForceDesktopMode()
    {
        lock (_lock)
        {
            IsPaused = true;
            ApplyDesktopProfile();
        }
    }

    public void Evaluate()
    {
        lock (_lock)
        {
            if (IsPaused)
            {
                return;
            }

            var detectedGame = _detector.DetectActiveGameProcess();

            if (!string.IsNullOrEmpty(detectedGame))
            {
                _gracePeriodDeadline = null;

                if (CurrentState != ProfileState.GamingHighPerformance || CurrentGameProcess != detectedGame)
                {
                    ApplyGamingProfile(detectedGame);
                }
            }
            else
            {
                if (CurrentState == ProfileState.GamingHighPerformance)
                {
                    if (_gracePeriodDeadline == null)
                    {
                        var graceSec = Math.Max(0, _options.Value.ExitGracePeriodSeconds);
                        _gracePeriodDeadline = DateTime.UtcNow.AddSeconds(graceSec);
                        _logger.LogInformation("Game closed. Starting grace period of {Seconds} seconds before switching to Balanced", graceSec);
                    }
                    else if (DateTime.UtcNow >= _gracePeriodDeadline)
                    {
                        _gracePeriodDeadline = null;
                        ApplyDesktopProfile();
                    }
                }
                else if (CurrentState != ProfileState.DesktopBalanced)
                {
                    ApplyDesktopProfile();
                }
            }
        }
    }

    private void ApplyGamingProfile(string gameProcessName)
    {
        CurrentState = ProfileState.GamingHighPerformance;
        CurrentGameProcess = gameProcessName;

        var config = _options.Value;
        _logger.LogInformation("Switching to GamingHighPerformance for game '{Game}'", gameProcessName);

        _powerManager.SetActivePlan(config.GamingPowerPlanGuid);
        _hardwareProvider.SetProfile(config.GamingHardwareProfile);

        StateChanged?.Invoke(CurrentState, CurrentGameProcess);
    }

    private void ApplyDesktopProfile()
    {
        CurrentState = ProfileState.DesktopBalanced;
        CurrentGameProcess = null;

        var config = _options.Value;
        _logger.LogInformation("Switching to DesktopBalanced mode");

        _powerManager.SetActivePlan(config.DesktopPowerPlanGuid);
        _hardwareProvider.SetProfile(config.DesktopHardwareProfile);

        StateChanged?.Invoke(CurrentState, null);
    }

    public void Dispose()
    {
        Stop();
    }
}
