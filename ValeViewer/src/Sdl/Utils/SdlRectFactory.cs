using static SDL2.SDL;

namespace ValeViewer.Sdl.Utils;

public static class SdlRectFactory
{
    public static SDL_Rect GetCenteredImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, out int zoom)
    {
        zoom = 100;
        if (imageWidth == 0 || imageHeight == 0)
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };

        return CreateCenteredRect(imageWidth, imageHeight, windowWidth, windowHeight);
    }

    public static SDL_Rect GetFittedImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, out int zoom)
    {
        if (imageWidth == 0 || imageHeight == 0)
        {
            zoom = 100;
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };
        }

        var scaleWidth = (float)windowWidth / imageWidth;
        var scaleHeight = (float)windowHeight / imageHeight;
        var finalScale = Math.Min(scaleWidth, scaleHeight);

        var scaledWidth = (int)(imageWidth * finalScale);
        var scaledHeight = (int)(imageHeight * finalScale);

        zoom = (int)Math.Round(finalScale * 100f, 0);
        return CreateCenteredRect(scaledWidth, scaledHeight, windowWidth, windowHeight);
    }

    public static SDL_Rect GetZoomedImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, int zoomPercent)
    {
        if (imageWidth == 0 || imageHeight == 0 || zoomPercent <= 0)
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };

        var newWidth = (int)(imageWidth * (zoomPercent / 100f));
        var newHeight = (int)(imageHeight * (zoomPercent / 100f));

        return CreateCenteredRect(newWidth, newHeight, windowWidth, windowHeight);
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