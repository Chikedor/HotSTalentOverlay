using System.Collections.Concurrent;

namespace HotSTalentOverlay.App;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly BlockingCollection<string> _queue = new(512);
    private readonly Task _writer;

    public FileLoggerProvider(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _writer = Task.Run(WriteLoop);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _queue);
    public void Dispose()
    {
        _queue.CompleteAdding();
        try { _writer.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _queue.Dispose();
    }

    private void WriteLoop()
    {
        using StreamWriter writer = new(_path, append: true) { AutoFlush = true };
        foreach (string line in _queue.GetConsumingEnumerable())
            writer.WriteLine(line);
    }

    private sealed class FileLogger(string category, BlockingCollection<string> queue) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= (category.StartsWith("Microsoft.", StringComparison.Ordinal) ? LogLevel.Warning : LogLevel.Information);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            string level = logLevel switch { LogLevel.Warning => "WARN", LogLevel.Error or LogLevel.Critical => "ERROR", _ => "INFO" };
            string detail = exception is null ? string.Empty : $" | {exception.GetType().Name}: {exception.Message}";
            queue.TryAdd($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} [{level}] {formatter(state, exception)}{detail} ({category})");
        }
    }
}
