using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using PowerFlow.Core.Models;
using PowerFlow.Core.Services;

namespace PowerFlow.App;

/// <summary>
/// System Tray application context managing the NotifyIcon, menus, and user actions.
/// </summary>
public sealed partial class TrayApplicationContext : ApplicationContext
{
    private readonly IProfileCoordinator _coordinator;
    private readonly ILogger<TrayApplicationContext> _logger;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _pauseResumeMenuItem;

    private Icon? _currentIcon;

    public TrayApplicationContext(
        IProfileCoordinator coordinator,
        ILogger<TrayApplicationContext> logger)
    {
        _coordinator = coordinator;
        _logger = logger;

        _contextMenu = new ContextMenuStrip();

        _statusMenuItem = new ToolStripMenuItem("PowerFlow: Starting...")
        {
            Enabled = false,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };

        var forceGamingItem = new ToolStripMenuItem("Force Gaming Mode", null, (_, _) => _coordinator.ForceGamingMode());
        var forceDesktopItem = new ToolStripMenuItem("Force Desktop Mode", null, (_, _) => _coordinator.ForceDesktopMode());

        _pauseResumeMenuItem = new ToolStripMenuItem("Pause Auto-Switching", null, (_, _) =>
        {
            if (_coordinator.IsPaused)
            {
                _coordinator.Resume();
            }
            else
            {
                _coordinator.Pause();
            }
        });

        var openConfigItem = new ToolStripMenuItem("Open Configuration (appsettings.json)", null, (_, _) => OpenConfigFile());
        var exitItem = new ToolStripMenuItem("Exit PowerFlow", null, (_, _) => ExitApp());

        _contextMenu.Items.Add(_statusMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(forceGamingItem);
        _contextMenu.Items.Add(forceDesktopItem);
        _contextMenu.Items.Add(_pauseResumeMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(openConfigItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Visible = true,
            Text = "PowerFlow: Initializing"
        };

        _coordinator.StateChanged += OnCoordinatorStateChanged;

        UpdateTrayVisuals(_coordinator.CurrentState, _coordinator.CurrentGameProcess);
        _coordinator.Start();
    }

    private void OnCoordinatorStateChanged(ProfileState state, string? gameName)
    {
        if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _notifyIcon.ContextMenuStrip.Invoke(() => UpdateTrayVisuals(state, gameName));
        }
        else
        {
            UpdateTrayVisuals(state, gameName);
        }
    }

    private void UpdateTrayVisuals(ProfileState state, string? gameName)
    {
        string statusText;
        Color badgeColor;

        switch (state)
        {
            case ProfileState.GamingHighPerformance:
                statusText = $"Gaming Mode ({gameName ?? "Active"})";
                badgeColor = Color.FromArgb(235, 75, 45); // Red / Orange
                _notifyIcon.Text = $"PowerFlow: Gaming ({gameName})";
                _notifyIcon.ShowBalloonTip(2000, "PowerFlow: Gaming Mode", $"High performance applied for {gameName}.", ToolTipIcon.Info);
                break;

            case ProfileState.Paused:
                statusText = "Auto-Switching Paused";
                badgeColor = Color.FromArgb(140, 140, 140); // Gray
                _notifyIcon.Text = "PowerFlow: Paused";
                break;

            default:
                statusText = "Desktop Mode (Balanced)";
                badgeColor = Color.FromArgb(40, 165, 230); // Teal / Blue
                _notifyIcon.Text = "PowerFlow: Desktop (Balanced)";
                break;
        }

        _statusMenuItem.Text = $"Status: {statusText}";
        _pauseResumeMenuItem.Text = _coordinator.IsPaused ? "Resume Auto-Switching" : "Pause Auto-Switching";

        UpdateTrayIcon(badgeColor);
    }

    private void UpdateTrayIcon(Color color)
    {
        try
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Draw solid outer badge ring
                using var brush = new SolidBrush(color);
                g.FillEllipse(brush, 2, 2, 28, 28);

                // Draw center white lightning bolt / power symbol
                using var pen = new Pen(Color.White, 3);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                var points = new Point[]
                {
                    new(18, 6),
                    new(12, 16),
                    new(17, 16),
                    new(14, 26)
                };
                g.DrawLines(pen, points);
            }

            var hIcon = bitmap.GetHicon();
            var newIcon = Icon.FromHandle(hIcon);

            var oldIcon = _currentIcon;
            _currentIcon = newIcon;
            _notifyIcon.Icon = newIcon;

            if (oldIcon != null)
            {
                DestroyIcon(oldIcon.Handle);
                oldIcon.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render dynamic system tray icon");
            _notifyIcon.Icon = SystemIcons.Application;
        }
    }

    private static void OpenConfigFile()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            Process.Start(new ProcessStartInfo("notepad.exe", configPath) { UseShellExecute = true });
        }
    }

    private void ExitApp()
    {
        _logger.LogInformation("Exiting PowerFlow");
        _coordinator.StateChanged -= OnCoordinatorStateChanged;
        _coordinator.Dispose();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        if (_currentIcon != null)
        {
            DestroyIcon(_currentIcon.Handle);
            _currentIcon.Dispose();
        }

        ExitThread();
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);
}
