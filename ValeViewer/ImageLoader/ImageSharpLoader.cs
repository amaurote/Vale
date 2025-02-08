using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ValeViewer.ImageLoader;

public class ImageSharpLoader : IImageLoader
{
    public UnmanagedImageData LoadImage(string imagePath)
    {
        using var image = Image.Load<Rgba32>(imagePath);
        var width = image.Width;
        var height = image.Height;
        int pixelDataSize = width * height * 4;

        IntPtr unmanagedBuffer = Marshal.AllocHGlobal(pixelDataSize);

        try
        {
            unsafe
            {
                byte* ptr = (byte*)unmanagedBuffer;
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