namespace HotSTalentOverlay.Core;

public static class ResilientFileCopy
{
    public static async Task<string> CreateSnapshotAsync(string source, string tempDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDirectory);
        string target = Path.Combine(tempDirectory, Guid.NewGuid().ToString("N") + Path.GetExtension(source));
        Exception? last = null;
        long previousLength = -1;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                FileInfo info = new(source);
                if (!info.Exists || info.Length == 0 || (previousLength >= 0 && info.Length != previousLength))
                {
                    previousLength = info.Exists ? info.Length : -1;
                    await Task.Delay(200 + (attempt * 150), cancellationToken);
                    continue;
                }
                using FileStream input = new(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using FileStream output = new(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
                return target;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(250 * (attempt + 1), cancellationToken);
            }
        }
        throw new IOException($"El archivo sigue bloqueado o incompleto después de varios intentos: {source}", last);
    }
}
