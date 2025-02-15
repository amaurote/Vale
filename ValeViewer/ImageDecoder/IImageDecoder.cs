namespace ValeViewer.ImageDecoder;

public interface IImageDecoder
{
    bool CanDecode(string extension);
    UnmanagedImageData Decode(string imagePath);
}