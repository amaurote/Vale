using System.Runtime.InteropServices;
using LibHeifSharp;
using ValeViewer.ImageDecoder.Utils;

namespace ValeViewer.ImageDecoder.Strategies;

public class HeifImageDecoder : IImageDecoder
{
    private static readonly string[] SupportedExtensions = [".heic", ".heif", ".avif"];

    public bool CanDecode(string extension)
    {
        return Array.Exists(SupportedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<UnmanagedImageData> DecodeAsync(string imagePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var heifContext = new HeifContext(imagePath);
                using var imageHandle = heifContext.GetPrimaryImageHandle();
                using var decodedImage = imageHandle.Decode(HeifColorspace.Rgb, HeifChroma.InterleavedRgba32);

                // Extract metadata
                var metadata = new Dictionary<string, string>();

                var exifData = imageHandle.GetExifMetadata();
                if (exifData != null)
                {
                    using var stream = new MemoryStream(exifData);
                    var parsedMetadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(stream);
                    metadata = MetadataProcessor.ProcessMetadata(parsedMetadata);
                }

                return ConvertToUnmanagedImage(decodedImage, metadata);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[HeifImageDecoder] Failed to load HEIF/AVIF image: {imagePath}", ex);
            }
        });
    }
    
    private unsafe UnmanagedImageData ConvertToUnmanagedImage(HeifImage decodedImage, Dictionary<string, string> metadata)
    {
        var width = decodedImage.Width;
        var height = decodedImage.Height;

        var planeData = decodedImage.GetPlane(HeifChannel.Interleaved);
        var scan0 = planeData.Scan0;
        var stride = planeData.Stride;

        var imageSize = stride * height;
        var unmanagedPtr = Marshal.AllocHGlobal(imageSize);

        Buffer.MemoryCopy((void*)scan0, (void*)unmanagedPtr, imageSize, imageSize);

        return new UnmanagedImageData(width, height, unmanagedPtr, Marshal.FreeHGlobal, metadata);
    }
}