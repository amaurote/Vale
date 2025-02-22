namespace ValeViewer.ImageDecoder;

public interface IImageDecoder
{
    bool CanDecode(string extension);
    Task<UnmanagedImageData> DecodeAsync(string imagePath);
}