using ValeViewer.Decoder.Strategies;

namespace ValeViewer.Decoder;

public static class ImageDecoderFactory
{
    // todo make proper DI
    // Lazy initialization
    private static readonly Lazy<IImageDecoder> ImageSharpDecoder = new(() => new ImageSharpDecoder());
    private static readonly Lazy<IImageDecoder> HeifImageDecoder = new(() => new HeifImageDecoder());

    private static readonly HashSet<IImageDecoder> ImageDecoder = [ImageSharpDecoder.Value, HeifImageDecoder.Value];

    public static Task<IImageDecoder> GetImageDecoderAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        try
        {
            var decoder = ImageDecoder.FirstOrDefault(it => it.CanDecode(extension));
            if (decoder == null)
                throw new ImageDecodeException($"[ImageDecoderFactory] File format '{extension}' is not supported.");

            return Task.FromResult(decoder);
        }
        catch (Exception ex)
        {
            throw new ImageDecodeException("[ImageDecoderFactory] Unhandled Exception.", ex);
        }
    }
}