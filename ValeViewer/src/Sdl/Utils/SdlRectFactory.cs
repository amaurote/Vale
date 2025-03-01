using static SDL2.SDL;

namespace ValeViewer.Sdl.Utils;

public static class SdlRectFactory
{
    public static SDL_Rect GetCenteredImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, out int zoom, float scaleFactor = 1.0f)
    {
        zoom = 100;
        if (imageWidth == 0 || imageHeight == 0)
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };

        var adjustedWidth = (int)(windowWidth / scaleFactor);
        var adjustedHeight = (int)(windowHeight / scaleFactor);

        return CreateCenteredRect(imageWidth, imageHeight, adjustedWidth, adjustedHeight);
    }

    public static SDL_Rect GetFittedImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, out int zoom, float scaleFactor = 1.0f)
    {
        if (imageWidth == 0 || imageHeight == 0)
        {
            zoom = 100;
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };
        }

        var adjustedWidth = (int)(windowWidth / scaleFactor);
        var adjustedHeight = (int)(windowHeight / scaleFactor);

        var scaleWidth = (float)adjustedWidth / imageWidth;
        var scaleHeight = (float)adjustedHeight / imageHeight;
        var finalScale = Math.Min(scaleWidth, scaleHeight);

        var scaledWidth = (int)(imageWidth * finalScale);
        var scaledHeight = (int)(imageHeight * finalScale);

        zoom = (int)Math.Round(finalScale * 100f, 0);
        return CreateCenteredRect(scaledWidth, scaledHeight, adjustedWidth, adjustedHeight);
    }

    public static SDL_Rect GetZoomedImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, int zoomPercent, float scaleFactor = 1.0f)
    {
        if (imageWidth == 0 || imageHeight == 0 || zoomPercent <= 0)
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };

        var adjustedWidth = (int)(windowWidth / scaleFactor);
        var adjustedHeight = (int)(windowHeight / scaleFactor);

        var newWidth = (int)(imageWidth * (zoomPercent / 100f));
        var newHeight = (int)(imageHeight * (zoomPercent / 100f));

        return CreateCenteredRect(newWidth, newHeight, adjustedWidth, adjustedHeight);
    }

    private static SDL_Rect CreateCenteredRect(int width, int height, int windowWidth, int windowHeight)
    {
        return new SDL_Rect
        {
            x = (windowWidth - width) / 2,
            y = (windowHeight - height) / 2,
            w = width,
            h = height
        };
    }
}