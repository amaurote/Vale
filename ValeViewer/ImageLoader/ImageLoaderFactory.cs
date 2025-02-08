using ValeViewer.ImageLoader.Strategies;

namespace ValeViewer.ImageLoader;

public static class ImageLoaderFactory
{
    // todo make proper DI
    private static readonly IImageLoader ImageSharpLoader = new ImageSharpLoader();
    private static readonly IImageLoader NetVipsLoader = new NetVipsImageLoader();

    private static HashSet<IImageLoader> _imageLoaders = [ImageSharpLoader, NetVipsLoader];

    public static IImageLoader GetImageLoader(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var loader = _imageLoaders.FirstOrDefault(il => il.CanLoad(extension));
        if (loader == null)
            throw new NotSupportedException($"File format '{extension}' is not supported.");

        return loader;
    }
}