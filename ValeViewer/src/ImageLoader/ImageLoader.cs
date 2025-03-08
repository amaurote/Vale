using ValeViewer.Static;

namespace ValeViewer.ImageLoader;

public class ImageLoader(IntPtr renderer)
{
    private readonly Dictionary<string, ImageComposite> _images = new();

    private const int PreloadDepth = 2;
    private const int CleanupSafeRange = 4;

    public void UpdateCollection()
    {
        Cleanup();

        var current = DirectoryNavigator.GetCurrent();
        if (current == null)
            return;

        var preload = DirectoryNavigator.GetAdjacent(PreloadDepth);

        if (!_images.ContainsKey(current))
        {
            _images.Add(current, new ImageComposite(current));
        }

        foreach (var path in preload.Where(path => !_images.ContainsKey(path)))
        {
            _images.Add(path, new ImageComposite(path));
        }
    }

    public async Task Preload()
    {
        foreach (var composite in _images.Where(composite => composite.Value.LoadState == CompositeState.Empty))
        {
            await composite.Value.LoadImageAsync(renderer);
        }
    }

    public ImageComposite? GetImageSynchronously()
    {
        var current = DirectoryNavigator.GetCurrent();
        if (current == null) return null;

        Logger.Log($"[ImageLoader] Loading image synchronously: {current}");

        if (!_images.TryGetValue(current, out var composite))
        {
            composite = new ImageComposite(current);
            _images[current] = composite;
            composite.LoadImageAsync(renderer).Wait();
        }

        Logger.Log($"[ImageLoader] Synchronous load complete.");
        return composite;
    }

    public ImageComposite? GetImage()
    {
        var current = DirectoryNavigator.GetCurrent();
        if (current == null) return null;

        if (!_images.TryGetValue(current, out var composite))
        {
            composite = new ImageComposite(current);
            _images[current] = composite;
            _ = composite.LoadImageAsync(renderer);
        }

        return composite;
    }

    private void Cleanup()
    {
        var current = DirectoryNavigator.GetCurrent();
        if (current == null)
        {
            DisposeAll();
            return;
        }

        var safeRange = new List<string> { current };
        safeRange.AddRange(DirectoryNavigator.GetAdjacent(CleanupSafeRange));

        var cleanupRange = _images.Keys.Where(x => !safeRange.Contains(x)).ToList();
        DisposeRange(cleanupRange);
    }

    private void DisposeRange(List<string> cleanupRange)
    {
        foreach (var path in cleanupRange)
        {
            if (_images.TryGetValue(path, out var composite))
            {
                composite.Dispose();
                _images.Remove(path);
            }
        }
    }

    public void DisposeAll()
    {
        foreach (var composite in _images.Values)
        {
            composite.Dispose();
        }

        _images.Clear();
    }
}