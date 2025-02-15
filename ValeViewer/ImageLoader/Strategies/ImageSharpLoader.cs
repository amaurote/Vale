using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace ValeViewer.ImageLoader.Strategies;

public class ImageSharpLoader : IImageLoader
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

    public bool CanLoad(string extension)
    {
        return Extensions.Contains(extension);
    }

    public UnmanagedImageData LoadImage(string imagePath)
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
            throw new ImageLoaderException($"[ImageSharpLoader] Failed to load image {imagePath}", ex);
        }
    }
}