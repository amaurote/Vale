namespace ValeViewer.ImageLoader;

public static class ImageLoaderFactory
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

    public static IImageLoader GetImageLoader(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        if (ImageSharpExtensions.Contains(extension))
            return new ImageSharpLoader();
        else
        {
            throw new NotSupportedException($"File format '{extension}' is not supported.");
        }
    }
}