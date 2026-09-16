using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace HotSTalentOverlay.Installer;

internal static class Program
{
    private const string AppName = "HotS Talent Overlay";
    private static readonly string InstallDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "HotSTalentOverlay");

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length > 0 && args[0].Equals("--verify", StringComparison.OrdinalIgnoreCase)) { VerifyPayload(); return; }
        if (args.Length > 0 && args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase)) { StartUninstall(); return; }
        if (args.Length > 1 && args[0].Equals("--uninstall-worker", StringComparison.OrdinalIgnoreCase)) { FinishUninstall(args[1]); return; }
        Application.Run(new InstallerForm());
    }

    private static void VerifyPayload()
    {
        using Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")
            ?? throw new InvalidOperationException("Installation payload is missing.");
        using ZipArchive archive = new(payload, ZipArchiveMode.Read);
        string[] required = ["HotSTalentOverlay.exe", "wwwroot/index.html", "tools/HeroesDataParser.exe"];
        foreach (string path in required)
            if (archive.GetEntry(path) is null && archive.GetEntry(path.Replace('/', '\\')) is null)
                throw new InvalidOperationException($"Installation payload is incomplete: {path}");
    }

    private sealed class InstallerForm : Form
    {
        private readonly CheckBox _desktop = new() { Text = "Crear acceso directo en el escritorio / Create desktop shortcut", Checked = true, AutoSize = true };
        private readonly CheckBox _startup = new() { Text = "Iniciar con Windows / Start with Windows", Checked = true, AutoSize = true };
        private readonly Button _install = new() { Text = "Instalar / Install", AutoSize = false, Height = 44, Dock = DockStyle.Bottom };
        private readonly Label _status = new() { Text = "Listo para instalar · Ready to install", AutoSize = true, ForeColor = Color.DimGray };

        public InstallerForm()
        {
            Text = $"{AppName} Setup";
            Width = 590;
            Height = 390;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(245, 244, 250);
            Font = new Font("Segoe UI", 10);

            Label title = new() { Text = AppName, Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true };
            Label intro = new() { Text = "Tus talentos en OBS, automáticamente.\nYour talents in OBS, automatically.", AutoSize = true, ForeColor = Color.FromArgb(70, 67, 82) };
            Label location = new() { Text = $"Se instalará en / Install location:\n{InstallDirectory}", AutoSize = true, ForeColor = Color.FromArgb(90, 87, 100) };
            FlowLayoutPanel content = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(30), AutoScroll = true };
            content.Controls.Add(title);
            content.Controls.Add(intro);
            content.SetFlowBreak(intro, true);
            content.Controls.Add(new Label { Height = 12, AutoSize = false });
            content.Controls.Add(location);
            content.Controls.Add(new Label { Height = 12, AutoSize = false });
            content.Controls.Add(_desktop);
            content.Controls.Add(_startup);
            content.Controls.Add(new Label { Height = 10, AutoSize = false });
            content.Controls.Add(_status);
            Controls.Add(content);
            Controls.Add(_install);
            _install.Click += InstallClicked;
        }

        private async void InstallClicked(object? sender, EventArgs eventArgs)
        {
            _install.Enabled = false;
            _status.Text = "Instalando… · Installing…";
            try
            {
                await Task.Run(() => Install(_desktop.Checked, _startup.Checked));
                _status.Text = "Instalación terminada ✓ · Installation complete ✓";
                _install.Text = "Abrir / Open";
                _install.Enabled = true;
                _install.Click -= InstallClicked;
                _install.Click += (_, _) => { LaunchInstalledApp(); Close(); };
            }
            catch (Exception ex)
            {
                _status.Text = "No se pudo instalar · Installation failed";
                _install.Enabled = true;
                MessageBox.Show(ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private static void Install(bool desktopShortcut, bool startWithWindows)
    {
        Directory.CreateDirectory(InstallDirectory);
        using Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")
            ?? throw new InvalidOperationException("No se encontró el paquete de instalación / Installation payload is missing.");
        using ZipArchive archive = new(payload, ZipArchiveMode.Read);
        archive.ExtractToDirectory(InstallDirectory, true);

        string currentInstaller = Environment.ProcessPath ?? throw new InvalidOperationException("Installer path unavailable.");
        File.Copy(currentInstaller, Path.Combine(InstallDirectory, "Uninstall.exe"), true);
        string appPath = Path.Combine(InstallDirectory, "HotSTalentOverlay.exe");
        string programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        CreateShortcut(Path.Combine(programs, $"{AppName}.lnk"), appPath, string.Empty);

        string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppName}.lnk");
        if (desktopShortcut) CreateShortcut(desktopLink, appPath, string.Empty); else DeleteIfExists(desktopLink);
        string startupLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{AppName}.lnk");
        if (startWithWindows) CreateShortcut(startupLink, appPath, "--background"); else DeleteIfExists(startupLink);

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\HotSTalentOverlay");
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", "1.3.1");
        key.SetValue("Publisher", "Chikedor");
        key.SetValue("InstallLocation", InstallDirectory);
        key.SetValue("DisplayIcon", appPath);
        key.SetValue("UninstallString", $"\"{Path.Combine(InstallDirectory, "Uninstall.exe")}\" --uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments)
    {
        Type type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
        dynamic shell = Activator.CreateInstance(type)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = InstallDirectory;
        shortcut.Description = AppName;
        shortcut.Save();
    }

    private static void LaunchInstalledApp() => Process.Start(new ProcessStartInfo(Path.Combine(InstallDirectory, "HotSTalentOverlay.exe")) { UseShellExecute = true });

    private static void StartUninstall()
    {
        if (MessageBox.Show("¿Desinstalar HotS Talent Overlay?\n\nUninstall HotS Talent Overlay?", AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        string worker = Path.Combine(Path.GetTempPath(), $"HotSTalentOverlay-Uninstall-{Guid.NewGuid():N}.exe");
        File.Copy(Environment.ProcessPath!, worker, true);
        Process.Start(new ProcessStartInfo(worker, $"--uninstall-worker \"{InstallDirectory}\"") { UseShellExecute = true });
    }

    private static void FinishUninstall(string directory)
    {
        Thread.Sleep(900);
        DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppName}.lnk"));
        DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", $"{AppName}.lnk"));
        DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{AppName}.lnk"));
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\HotSTalentOverlay", false);
        try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
        MessageBox.Show("HotS Talent Overlay se ha desinstalado.\nHotS Talent Overlay has been uninstalled.", AppName);
    }

    private static void DeleteIfExists(string path) { if (File.Exists(path)) File.Delete(path); }
}
