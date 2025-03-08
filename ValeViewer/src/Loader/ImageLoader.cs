using System.Collections.Concurrent;

namespace ValeViewer.Loader;

public class ImageLoader(IntPtr renderer)
{
    private readonly ConcurrentDictionary<string, ImageComposite> _images = new();

    private const int PreloadDepth = 2;
    private const int CleanupSafeRange = 3;

    public void UpdateCollection()
    {
        Cleanup();

        var current = DirectoryNavigator.GetCurrent();
        if (current == null)
            return;

        var preload = DirectoryNavigator.GetAdjacent(PreloadDepth);
        _images.TryAdd(current, new ImageComposite(current));

        foreach (var path in preload)
        {
            _images.TryAdd(path, new ImageComposite(path));
        }
    }

    public async Task Preload()
    {
        var tasks = _images.Values
            .Where(composite => composite.LoadState == CompositeState.Empty)
            .Select(composite => composite.LoadImageAsync(renderer));

        await Task.WhenAll(tasks);
    }

    public ImageComposite GetImage()
    {
        var current = DirectoryNavigator.GetCurrent();
        if (current == null) 
            return new ImageComposite();

        return _images.GetOrAdd(current, key => new ImageComposite(key));
    }

    private void Cleanup()
    {
        var current = DirectoryNavigator.GetCurrent();
        if (current == null)
        {
            DisposeAll();
            return;
        }

        var safeRange = new HashSet<string> { current };
        safeRange.UnionWith(DirectoryNavigator.GetAdjacent(CleanupSafeRange));

        foreach (var key in _images.Keys.Except(safeRange).ToList())
        {
            if (_images.TryRemove(key, out var composite))
                composite.Dispose();
        }
    }

    public void DisposeAll()
    {
        foreach (var composite in _images.Values)
            composite.Dispose();

        _images.Clear();
    }
}