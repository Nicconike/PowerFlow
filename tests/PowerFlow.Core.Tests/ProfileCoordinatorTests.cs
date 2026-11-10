using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PowerFlow.Core.Configuration;
using PowerFlow.Core.Models;
using PowerFlow.Core.Providers;
using PowerFlow.Core.Services;
using Xunit;

namespace PowerFlow.Core.Tests;

public class ProfileCoordinatorTests
{
    private readonly IProcessDetector _detector = Substitute.For<IProcessDetector>();
    private readonly IPowerPlanManager _powerManager = Substitute.For<IPowerPlanManager>();
    private readonly IHardwareProfileProvider _hardwareProvider = Substitute.For<IHardwareProfileProvider>();
    private readonly PowerFlowOptions _options = new()
    {
        PollIntervalSeconds = 1,
        ExitGracePeriodSeconds = 2,
        GamingPowerPlanGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
        DesktopPowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2e",
        GamingHardwareProfile = HardwareProfile.Performance,
        DesktopHardwareProfile = HardwareProfile.Balance
    };

    private ProfileCoordinator CreateCoordinator()
    {
        return new ProfileCoordinator(
            _detector,
            _powerManager,
            _hardwareProvider,
            Options.Create(_options),
            NullLogger<ProfileCoordinator>.Instance);
    }

    [Fact]
    public void Evaluate_WhenGameDetected_SwitchesToGamingProfile()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        _detector.DetectActiveGameProcess().Returns("PAYDAY3-Win64-Shipping");

        // Act
        coordinator.Evaluate();

        // Assert
        coordinator.CurrentState.Should().Be(ProfileState.GamingHighPerformance);
        coordinator.CurrentGameProcess.Should().Be("PAYDAY3-Win64-Shipping");

        _powerManager.Received(1).SetActivePlan(_options.GamingPowerPlanGuid);
        _hardwareProvider.Received(1).SetProfile(_options.GamingHardwareProfile);
    }

    [Fact]
    public void Evaluate_WhenGameExits_EntersGracePeriodBeforeSwitchingToDesktop()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        _detector.DetectActiveGameProcess().Returns("PAYDAY3-Win64-Shipping");
        coordinator.Evaluate();

        // Act 1: Game closes, first evaluation starts grace period
        _detector.DetectActiveGameProcess().Returns((string?)null);
        coordinator.Evaluate();

        // Assert 1: Still in gaming mode during grace period
        coordinator.CurrentState.Should().Be(ProfileState.GamingHighPerformance);
        _powerManager.Received(0).SetActivePlan(_options.DesktopPowerPlanGuid);

        // Act 2: Simulate waiting past grace period (wait 2.1s)
        Thread.Sleep(2100);
        coordinator.Evaluate();

        // Assert 2: Switched back to desktop balanced
        coordinator.CurrentState.Should().Be(ProfileState.DesktopBalanced);
        coordinator.CurrentGameProcess.Should().BeNull();
        _powerManager.Received(1).SetActivePlan(_options.DesktopPowerPlanGuid);
        _hardwareProvider.Received(1).SetProfile(_options.DesktopHardwareProfile);
    }

    [Fact]
    public void Pause_PreventsAutomaticProfileChanges()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        coordinator.Pause();
        _detector.DetectActiveGameProcess().Returns("PAYDAY3-Win64-Shipping");

        // Act
        coordinator.Evaluate();

        // Assert
        coordinator.IsPaused.Should().BeTrue();
        coordinator.CurrentState.Should().Be(ProfileState.Paused);
        _powerManager.DidNotReceive().SetActivePlan(Arg.Any<string>());
    }

    [Fact]
    public void ForceGamingMode_AppliesGamingProfileImmediately()
    {
        // Arrange
        var coordinator = CreateCoordinator();

        // Act
        coordinator.ForceGamingMode();

        // Assert
        coordinator.IsPaused.Should().BeTrue();
        coordinator.CurrentState.Should().Be(ProfileState.GamingHighPerformance);
        _powerManager.Received(1).SetActivePlan(_options.GamingPowerPlanGuid);
        _hardwareProvider.Received(1).SetProfile(_options.GamingHardwareProfile);
    }

    [Fact]
    public void ForceDesktopMode_AppliesDesktopProfileImmediately()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        _detector.DetectActiveGameProcess().Returns("PAYDAY3-Win64-Shipping");
        coordinator.Evaluate();

        // Act
        coordinator.ForceDesktopMode();

        // Assert
        coordinator.IsPaused.Should().BeTrue();
        coordinator.CurrentState.Should().Be(ProfileState.DesktopBalanced);
        _powerManager.Received(1).SetActivePlan(_options.DesktopPowerPlanGuid);
        _hardwareProvider.Received(1).SetProfile(_options.DesktopHardwareProfile);
    }
}
