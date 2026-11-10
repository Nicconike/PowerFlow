using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PowerFlow.Core.Models;
using PowerFlow.Core.Providers.Desktop;
using Xunit;

namespace PowerFlow.Core.Tests;

public class DesktopProviderTests
{
    [Fact]
    public void DesktopProvider_ReportsGenericDesktopBrand()
    {
        // Arrange & Act
        var provider = new DesktopProvider(NullLogger<DesktopProvider>.Instance);

        // Assert
        provider.Brand.Should().Be(HardwareBrand.GenericDesktop);
        provider.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void SetProfile_UpdatesCurrentProfile()
    {
        // Arrange
        var provider = new DesktopProvider(NullLogger<DesktopProvider>.Instance);

        // Act
        var result = provider.SetProfile(HardwareProfile.Performance);

        // Assert
        result.Should().BeTrue();
        provider.GetCurrentProfile().Should().Be(HardwareProfile.Performance);
    }
}
