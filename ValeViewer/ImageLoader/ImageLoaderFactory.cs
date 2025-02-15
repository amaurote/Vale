using ValeViewer.ImageLoader.Strategies;

namespace ValeViewer.ImageLoader;

public static class ImageLoaderFactory
{
    // todo make proper DI
    // Lazy initialization
    private static readonly Lazy<IImageLoader> ImageSharpLoader = new(() => new ImageSharpLoader());
    private static readonly Lazy<IImageLoader> HeifImageLoader = new(() => new HeifImageLoader());

    private static readonly HashSet<IImageLoader> ImageLoaders = [ImageSharpLoader.Value, HeifImageLoader.Value];

    public static IImageLoader GetImageLoader(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        IImageLoader loader = null!;
        try
        {
            loader = ImageLoaders.FirstOrDefault(it => it.CanLoad(extension))
                     ?? throw new NotSupportedException($"[ImageLoaderFactory] File format '{extension}' is not supported.");
        }
        catch (Exception ex)
        {
            Logger.Log($"[ImageLoaderFactory] Unhandled Exception: {ex}", Logger.LogLevel.Error);
        }

        return loader;
    }
}