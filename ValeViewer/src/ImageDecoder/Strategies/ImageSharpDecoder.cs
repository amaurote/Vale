using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace ValeViewer.ImageDecoder.Strategies;

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

    public UnmanagedImageData Decode(string imagePath)
    {
        var decoderOptions = new DecoderOptions { Configuration = Configuration.Default };

        using var stream = File.OpenRead(imagePath);
        using var image = Image.Load<Rgba32>(decoderOptions, stream);
        
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

            return new UnmanagedImageData(width, height, unmanagedBuffer, Marshal.FreeHGlobal);
        }
        catch(Exception ex)
        {
            Marshal.FreeHGlobal(unmanagedBuffer);
            throw new ImageDecodeException($"[ImageSharpDecoder] Failed to load image {imagePath}", ex);
        }
    }
}