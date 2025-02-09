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
                     ?? throw new NotSupportedException($"File format '{extension}' is not supported.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unhandled Exception!");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
            }
        }

        return loader;
    }
}