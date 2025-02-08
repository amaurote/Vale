namespace ValeViewer.ImageLoader;

public interface IImageLoader
{
    UnmanagedImageData LoadImage(string imagePath);
}