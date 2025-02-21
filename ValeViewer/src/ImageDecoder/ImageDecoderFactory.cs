using ValeViewer.ImageDecoder.Strategies;

namespace ValeViewer.ImageDecoder;

public static class ImageLoaderFactory
{
    // todo make proper DI
    // Lazy initialization
    private static readonly Lazy<IImageDecoder> ImageSharpLoader = new(() => new ImageSharpDecoder());
    private static readonly Lazy<IImageDecoder> HeifImageLoader = new(() => new HeifImageDecoder());

    private static readonly HashSet<IImageDecoder> ImageLoaders = [ImageSharpLoader.Value, HeifImageLoader.Value];

    public static IImageDecoder GetImageDecoder(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        IImageDecoder? decoder = null;
        try
        {
            decoder = ImageLoaders.FirstOrDefault(it => it.CanDecode(extension));
            if (decoder == null)
                throw new ImageDecodeException($"[ImageDecoderFactory] File format '{extension}' is not supported.");
        }
        catch (Exception ex)
        {
            throw new ImageDecodeException($"[ImageDecoderFactory] Unhandled Exception.", ex);
        }

        return decoder;
    }
}