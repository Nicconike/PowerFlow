using PowerFlow.Core.Models;

namespace PowerFlow.Core.Configuration;

/// <summary>
/// Configuration options for PowerFlow process monitoring and power switching.
/// </summary>
public sealed class PowerFlowOptions
{
    public const string SectionName = "PowerFlow";

    public int PollIntervalSeconds { get; set; } = 4;

    public int ExitGracePeriodSeconds { get; set; } = 8;

    public string GamingPowerPlanGuid { get; set; } = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    public string DesktopPowerPlanGuid { get; set; } = "381b4222-f694-41f0-9685-ff5bb260df2e";

    public HardwareProfile GamingHardwareProfile { get; set; } = HardwareProfile.Performance;

    public HardwareProfile DesktopHardwareProfile { get; set; } = HardwareProfile.Balance;

    public List<string> GameDirectoryKeywords { get; set; } =
    [
        @"steamapps\common",
        "Epic Games",
        "XboxGames",
        "Riot Games",
        "Ubisoft Game Launcher",
        @"GOG Galaxy\Games"
    ];

    public List<string> CustomProcessNames { get; set; } =
    [
        "PAYDAY3-Win64-Shipping",
        "payday3"
    ];

    public List<string> IgnoredProcesses { get; set; } =
    [
        "steam",
        "steamwebhelper",
        "epicgameslauncher",
        "riotclientservices",
        "discord",
        "devenv",
        "code",
        "antigravity"
    ];
}
