using System.Diagnostics;
using System.Text.Json;

namespace HotSTalentOverlay.Core;

public sealed class CatalogBuilder
{
    private readonly AppPaths _paths;

    public CatalogBuilder(AppPaths paths) => _paths = paths;

    public async Task<CatalogDocument> GenerateAsync(
        DetectedGame game,
        string locale,
        string helperPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(helperPath))
            throw new FileNotFoundException("No se encuentra HeroesDataParser. Ejecuta setup.ps1.", helperPath);

        string staging = Path.Combine(_paths.Temp, "catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            ProcessStartInfo start = new(helperPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (string argument in new[]
            {
                "game", "--storage-path", game.Path, "--extractor", "hero:i", "--localization", locale,
                "--gamestring-text", "PlainText", "--localized-text", "Copy", "--output-path", staging,
            })
                start.ArgumentList.Add(argument);

            using Process process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar HeroesDataParser.");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            string output = await outputTask;
            string error = await errorTask;
            progress?.Report(output);
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"HeroesDataParser terminó con código {process.ExitCode}: {LastUsefulLine(error + Environment.NewLine + output)}");

            string sourceJson = Directory.EnumerateFiles(Path.Combine(staging, "data"), "herodata_*.json").Single();
            return await ImportAsync(sourceJson, Path.Combine(staging, "images", "abilitytalents"), locale, cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    public async Task<CatalogDocument> ImportAsync(string sourceJson, string sourceImages, string locale, CancellationToken cancellationToken = default)
    {
        using JsonDocument source = JsonDocument.Parse(await File.ReadAllTextAsync(sourceJson, cancellationToken));
        JsonElement meta = source.RootElement.GetProperty("meta");
        string version = meta.GetProperty("heroesVersion").GetString() ?? throw new InvalidDataException("Falta heroesVersion.");
        int build = int.Parse(version.Split('.').Last());
        JsonElement items = source.RootElement.GetProperty("items");
        List<TalentCatalogEntry> talents = [];
        HashSet<string> images = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty heroProperty in items.EnumerateObject())
        {
            JsonElement hero = heroProperty.Value;
            if (!hero.TryGetProperty("talents", out JsonElement tiers))
                continue;
            string heroName = hero.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? heroProperty.Name : heroProperty.Name;
            string heroUnitId = hero.TryGetProperty("unitId", out JsonElement unit) ? unit.GetString() ?? heroProperty.Name : heroProperty.Name;
            foreach (JsonProperty tier in tiers.EnumerateObject())
            {
                if (!TryParseLevel(tier.Name, out int level))
                    continue;
                foreach (JsonElement talent in tier.Value.EnumerateArray())
                {
                    string id = talent.GetProperty("talentId").GetString() ?? string.Empty;
                    string icon = talent.GetProperty("icon").GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(icon))
                        continue;
                    images.Add(icon);
                    talents.Add(new TalentCatalogEntry(id, heroUnitId, heroName, level,
                        talent.GetProperty("name").GetString() ?? id, $"assets/talents/{icon}"));
                }
            }
        }

        string newAssets = Path.Combine(_paths.Temp, "talents-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(newAssets);
        foreach (string image in images)
        {
            string sourceImage = Path.Combine(sourceImages, image);
            if (!File.Exists(sourceImage))
                throw new InvalidDataException($"El extractor no generó el icono requerido: {image}");
            File.Copy(sourceImage, Path.Combine(newAssets, image));
        }

        CatalogDocument document = new(version, build, DateTimeOffset.UtcNow.ToString("O"), locale,
            items.EnumerateObject().Count(), talents.OrderBy(x => x.Hero).ThenBy(x => x.Level).ThenBy(x => x.Name).ToArray());
        string newCatalog = Path.Combine(_paths.Temp, "talents-" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(newCatalog, JsonSerializer.Serialize(document, JsonOptions.Indented), cancellationToken);

        string backupAssets = _paths.TalentAssets + ".previous";
        TryDeleteDirectory(backupAssets);
        if (Directory.Exists(_paths.TalentAssets))
            Directory.Move(_paths.TalentAssets, backupAssets);
        try
        {
            Directory.Move(newAssets, _paths.TalentAssets);
            File.Move(newCatalog, _paths.CatalogFile, true);
            TryDeleteDirectory(backupAssets);
        }
        catch
        {
            TryDeleteDirectory(_paths.TalentAssets);
            if (Directory.Exists(backupAssets))
                Directory.Move(backupAssets, _paths.TalentAssets);
            throw;
        }

        return document;
    }

    private static bool TryParseLevel(string value, out int level) => int.TryParse(value.AsSpan("Level".Length), out level);
    private static string LastUsefulLine(string text) => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Error desconocido";
    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }
}

