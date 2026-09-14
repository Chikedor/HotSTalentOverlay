using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using HotSTalentOverlay.Core;

namespace HotSTalentOverlay.App;

public sealed class EventHub
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();

    public void Publish(AppStatus status)
    {
        string json = JsonSerializer.Serialize(status, JsonOptions.Default);
        foreach (Channel<string> channel in _subscribers.Values)
            channel.Writer.TryWrite(json);
    }

    public async IAsyncEnumerable<string> Subscribe([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Guid id = Guid.NewGuid();
        Channel<string> channel = Channel.CreateBounded<string>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
        _subscribers[id] = channel;
        try
        {
            await foreach (string json in channel.Reader.ReadAllAsync(cancellationToken))
                yield return json;
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }
}

