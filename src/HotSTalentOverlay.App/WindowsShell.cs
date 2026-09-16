using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace HotSTalentOverlay.App;

public static class WindowsShell
{
    private static Thread? _trayThread;

    public static void Start(IHostApplicationLifetime lifetime, int port, bool openBrowser)
    {
        string dashboardUrl = $"http://127.0.0.1:{port}/";
        if (openBrowser) OpenDashboard(dashboardUrl);

        _trayThread = new Thread(() => RunTray(lifetime, dashboardUrl))
        {
            IsBackground = true,
            Name = "HotS Talent Overlay tray",
        };
        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.Start();
    }

    private static void RunTray(IHostApplicationLifetime lifetime, string dashboardUrl)
    {
        using NotifyIcon tray = new()
        {
            Icon = SystemIcons.Application,
            Text = "HotS Talent Overlay",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };
        tray.ContextMenuStrip.Items.Add("Abrir panel / Open dashboard", null, (_, _) => OpenDashboard(dashboardUrl));
        tray.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        tray.ContextMenuStrip.Items.Add("Salir / Exit", null, (_, _) => lifetime.StopApplication());
        tray.DoubleClick += (_, _) => OpenDashboard(dashboardUrl);
        lifetime.ApplicationStopping.Register(() => System.Windows.Forms.Application.ExitThread());
        System.Windows.Forms.Application.Run();
        tray.Visible = false;
    }

    private static void OpenDashboard(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
