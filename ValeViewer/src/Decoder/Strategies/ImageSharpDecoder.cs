using System.Runtime.InteropServices;
using MetadataExtractor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using ValeViewer.Decoder.Utils;

namespace ValeViewer.Decoder.Strategies;

public class ImageSharpDecoder : IImageDecoder
{
    private static readonly string[] Extensions =
    [
        ".bmp",
        ".jpeg", ".jpg",
        ".png",
        ".tga",
        ".tiff",
        ".webp"
    ];

    public bool CanDecode(string extension)
    {
        return Extensions.Contains(extension);
    }

    public async Task<UnmanagedImageData> DecodeAsync(string imagePath)
    {
        var decoderOptions = new DecoderOptions { Configuration = Configuration.Default };

        await using var stream = File.OpenRead(imagePath);
        using var image = await Image.LoadAsync<Rgba32>(decoderOptions, stream);

        var width = image.Width;
        var height = image.Height;
        var pixelDataSize = width * height * 4;

        var unmanagedBuffer = Marshal.AllocHGlobal(pixelDataSize);

        try
        {
            unsafe
            {
                var ptr = (byte*)unmanagedBuffer;
                image.CopyPixelDataTo(new Span<byte>(ptr, pixelDataSize));
            }
            
            var parsed = ImageMetadataReader.ReadMetadata(imagePath);
            var metadata = MetadataProcessor.ProcessMetadata(parsed);

            return new UnmanagedImageData(width, height, unmanagedBuffer, Marshal.FreeHGlobal, metadata);
        }
        catch (Exception ex)
        {
            Marshal.FreeHGlobal(unmanagedBuffer);
            throw new ImageDecodeException($"[ImageSharpDecoder] Failed to load image {imagePath}", ex);
        }
    }
}