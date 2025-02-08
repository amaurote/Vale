using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ValeViewer.ImageLoader.Strategies;

public class ImageSharpLoader : IImageLoader
{
    private static readonly string[] ImageSharpExtensions =
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
        return ImageSharpExtensions.Contains(extension);
    }

    public UnmanagedImageData LoadImage(string imagePath)
    {
        using var image = Image.Load<Rgba32>(imagePath);
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
        catch
        {
            Marshal.FreeHGlobal(unmanagedBuffer);
            throw;
        }
    }
}