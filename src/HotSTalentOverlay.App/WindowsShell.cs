using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace HotSTalentOverlay.App;

public static class WindowsShell
{
    private const string TrayTitle = "HotS Talent Overlay";
    private static Thread? _trayThread;

    public static void Start(IHostApplicationLifetime lifetime, UpdateService updates, int port, bool openBrowser)
    {
        string dashboardUrl = $"http://127.0.0.1:{port}/";
        if (openBrowser) OpenDashboard(dashboardUrl);

        _trayThread = new Thread(() => RunTray(lifetime, updates, dashboardUrl))
        {
            IsBackground = true,
            Name = "HotS Talent Overlay tray",
        };
        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.Start();
    }

    private static void RunTray(IHostApplicationLifetime lifetime, UpdateService updates, string dashboardUrl)
    {
        ContextMenuStrip menu = new();
        using NotifyIcon tray = new()
        {
            Icon = SystemIcons.Application,
            Text = $"{TrayTitle} v{UpdateService.CurrentVersion}",
            Visible = true,
            ContextMenuStrip = menu,
        };
        ToolStripMenuItem update = new() { Visible = false, Font = new Font(menu.Font, FontStyle.Bold) };
        menu.Items.Add(new ToolStripMenuItem($"Versión instalada / Installed version: {UpdateService.CurrentVersion}") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        ToolStripSeparator updateSeparator = new() { Visible = false };
        menu.Items.Add(update);
        menu.Items.Add(updateSeparator);
        menu.Items.Add("Abrir panel / Open dashboard", null, (_, _) => OpenDashboard(dashboardUrl));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir / Exit", null, (_, _) => lifetime.StopApplication());
        tray.DoubleClick += (_, _) => OpenDashboard(dashboardUrl);

        bool installing = false;
        void ShowUpdate(UpdateStatus status)
        {
            if (installing) return;
            update.Visible = updateSeparator.Visible = status.Available;
            update.Enabled = true;
            update.Text = $"Actualizar a {status.LatestVersion} y reiniciar / Update to {status.LatestVersion} and restart";
            tray.Text = $"{TrayTitle} v{UpdateService.CurrentVersion}";
        }
        async void RefreshUpdate(bool force)
        {
            try { ShowUpdate(await updates.CheckAsync(force, lifetime.ApplicationStopping)); }
            catch { }
        }
        update.Click += async (_, _) =>
        {
            installing = true;
            update.Enabled = false;
            update.Text = "Descargando actualización… / Downloading update…";
            try { await updates.InstallAsync(lifetime.ApplicationStopping); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                installing = false;
                RefreshUpdate(false);
                tray.ShowBalloonTip(5000, TrayTitle, ex.Message, ToolTipIcon.Error);
            }
        };
        menu.Opening += (_, _) => RefreshUpdate(false);

        // UpdateService caches for 15 minutes, so an hourly check matches the dashboard's polling.
        using System.Windows.Forms.Timer timer = new() { Interval = (int)TimeSpan.FromHours(1).TotalMilliseconds };
        timer.Tick += (_, _) => RefreshUpdate(false);
        timer.Start();
        RefreshUpdate(false);

        // ApplicationStopping fires on another thread; ExitThread must run on this one to end its message loop.
        SynchronizationContext ui = SynchronizationContext.Current ?? throw new InvalidOperationException("Tray thread has no UI context.");
        lifetime.ApplicationStopping.Register(() => ui.Post(_ => System.Windows.Forms.Application.ExitThread(), null));
        System.Windows.Forms.Application.Run();
        tray.Visible = false;
    }

    private static void OpenDashboard(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
