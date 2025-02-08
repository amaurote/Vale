namespace ValeViewer.ImageLoader;

public interface IImageLoader
{
    bool CanLoad(string extension);
    UnmanagedImageData LoadImage(string imagePath);
}