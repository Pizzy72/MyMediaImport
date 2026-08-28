using System.Windows.Media.Imaging;

namespace MyMediaImport.App;

public sealed class ThumbnailMemoryCache
{
    private readonly int _capacity;
    private readonly Dictionary<string, CacheEntry> _entries = [];
    private readonly LinkedList<string> _usage = [];
    private readonly object _syncRoot = new();

    public ThumbnailMemoryCache(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
    }

    public bool TryGet(string key, out BitmapSource? image)
    {
        lock (_syncRoot)
        {
            if (!_entries.TryGetValue(key, out CacheEntry? entry))
            {
                image = null;
                return false;
            }

            _usage.Remove(entry.UsageNode);
            _usage.AddFirst(entry.UsageNode);
            image = entry.Image;
            return true;
        }
    }

    public void Add(string key, BitmapSource image)
    {
        lock (_syncRoot)
        {
            if (_entries.Remove(key, out CacheEntry? existing))
            {
                _usage.Remove(existing.UsageNode);
            }

            LinkedListNode<string> node = _usage.AddFirst(key);
            _entries.Add(key, new(image, node));

            while (_entries.Count > _capacity)
            {
                LinkedListNode<string>? last = _usage.Last;
                if (last is null)
                {
                    break;
                }

                _usage.RemoveLast();
                _entries.Remove(last.Value);
            }
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _entries.Clear();
            _usage.Clear();
        }
    }

    private sealed record CacheEntry(
        BitmapSource Image,
        LinkedListNode<string> UsageNode);
}
