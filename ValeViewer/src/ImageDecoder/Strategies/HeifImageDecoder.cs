using System.Runtime.InteropServices;
using LibHeifSharp;

namespace ValeViewer.ImageDecoder.Strategies;

public class HeifImageDecoder : IImageDecoder
{
    private static readonly string[] SupportedExtensions = [".heic", ".heif", ".avif"];

    public bool CanDecode(string extension)
    {
        return Array.Exists(SupportedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public unsafe UnmanagedImageData Decode(string imagePath)
    {
        try
        {
            using var heifContext = new HeifContext(imagePath);
            using var imageHandle = heifContext.GetPrimaryImageHandle();
            using var decodedImage = imageHandle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgba32);

            return ConvertToUnmanagedImage(decodedImage);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[HeifImageDecoder] Failed to load HEIF/AVIF image: {imagePath}", ex);
        }
    }

    private unsafe UnmanagedImageData ConvertToUnmanagedImage(HeifImage decodedImage)
    {
        var width = decodedImage.Width;
        var height = decodedImage.Height;

        var planeData = decodedImage.GetPlane(HeifChannel.Interleaved);
        var scan0 = planeData.Scan0;
        var stride = planeData.Stride;

        var imageSize = stride * height;
        var unmanagedPtr = Marshal.AllocHGlobal(imageSize);

        Buffer.MemoryCopy((void*)scan0, (void*)unmanagedPtr, imageSize, imageSize);

        return new UnmanagedImageData(width, height, unmanagedPtr, Marshal.FreeHGlobal);
    }
}