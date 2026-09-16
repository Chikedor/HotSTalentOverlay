using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using HotSTalentOverlay.Core;
using Microsoft.Extensions.Hosting;

namespace HotSTalentOverlay.App;

public sealed record UpdateStatus(
    string CurrentVersion,
    string LatestVersion,
    bool Available,
    string ReleaseUrl,
    DateTimeOffset? CheckedAt,
    string Error = "");

public sealed class UpdateService(HttpClient http, IHostApplicationLifetime lifetime, ILogger<UpdateService> logger)
{
    private const string LatestReleaseApi = "https://api.github.com/repos/Chikedor/HotSTalentOverlay/releases/latest";
    private const string InstallerAssetName = "HotSTalentOverlay-Setup.exe";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UpdateStatus? _cached;
    private string? _installerUrl;

    public async Task<UpdateStatus> CheckAsync(bool force, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!force && _cached?.CheckedAt is DateTimeOffset checkedAt && DateTimeOffset.UtcNow - checkedAt < TimeSpan.FromMinutes(15))
                return _cached;

            string currentText = GetCurrentVersion().ToString(3);
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, LatestReleaseApi);
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("HotSTalentOverlay", currentText));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

                JsonElement root = json.RootElement;
                string tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
                Version latest = ReleaseVersionParser.Parse(tag) ?? throw new InvalidDataException("La release no contiene una versión válida.");
                string releaseUrl = root.TryGetProperty("html_url", out JsonElement page) ? page.GetString() ?? string.Empty : string.Empty;
                _installerUrl = null;
                foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray())
                {
                    if (!string.Equals(asset.GetProperty("name").GetString(), InstallerAssetName, StringComparison.OrdinalIgnoreCase)) continue;
                    _installerUrl = asset.TryGetProperty("browser_download_url", out JsonElement download) ? download.GetString() : null;
                    break;
                }

                bool available = latest > GetCurrentVersion() && !string.IsNullOrWhiteSpace(_installerUrl);
                _cached = new(currentText, latest.ToString(3), available, releaseUrl, DateTimeOffset.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "No se pudo comprobar si hay actualizaciones");
                _cached = new(currentText, string.Empty, false, string.Empty, DateTimeOffset.UtcNow, ex.Message);
                _installerUrl = null;
            }
            return _cached;
        }
        finally { _gate.Release(); }
    }

    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        UpdateStatus status = await CheckAsync(true, cancellationToken);
        if (!status.Available || string.IsNullOrWhiteSpace(_installerUrl))
            throw new InvalidOperationException("No hay ninguna actualización disponible.");

        Uri downloadUri = new(_installerUrl);
        if (!downloadUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !downloadUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("GitHub devolvió una descarga no válida.");

        string updateDirectory = Path.Combine(Path.GetTempPath(), "HotSTalentOverlay", status.LatestVersion);
        Directory.CreateDirectory(updateDirectory);
        string installerPath = Path.Combine(updateDirectory, InstallerAssetName);
        string partialPath = installerPath + ".download";

        using (HttpRequestMessage request = new(HttpMethod.Get, downloadUri))
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("HotSTalentOverlay", status.CurrentVersion));
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream output = new(partialPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await input.CopyToAsync(output, cancellationToken);
        }

        FileInfo downloaded = new(partialPath);
        if (downloaded.Length < 1_000_000 || !await HasExecutableHeaderAsync(partialPath, cancellationToken))
            throw new InvalidDataException("El instalador descargado no es válido.");
        File.Move(partialPath, installerPath, true);

        ProcessStartInfo start = new(installerPath) { UseShellExecute = true };
        start.ArgumentList.Add("--update");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        _ = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar el actualizador.");
        _ = Task.Run(async () => { await Task.Delay(700); lifetime.StopApplication(); });
    }

    private static Version GetCurrentVersion() => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

    private static async Task<bool> HasExecutableHeaderAsync(string path, CancellationToken cancellationToken)
    {
        byte[] header = new byte[2];
        await using FileStream stream = File.OpenRead(path);
        return await stream.ReadAsync(header, cancellationToken) == 2 && header[0] == (byte)'M' && header[1] == (byte)'Z';
    }
}
