using static SDL2.SDL;

namespace ValeViewer.Sdl;

public static class SdlRectFactory
{
    public static SDL_Rect GetCenteredImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight)
    {
        return new SDL_Rect
        {
            x = (windowWidth - imageWidth) / 2,
            y = (windowHeight - imageHeight) / 2,
            w = imageWidth,
            h = imageHeight
        };
    }
    
    public static SDL_Rect GetFittedImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight) // todo out calculated zoom
    {
        var imageAspect = (float)imageWidth / imageHeight;
        var screenAspect = (float)windowWidth / windowHeight;

        int newWidth, newHeight;

        if (imageAspect > screenAspect)
        {
            // Image is wider than screen, fit by width
            newWidth = windowWidth;
            newHeight = (int)(windowWidth / imageAspect);
        }
        else
        {
            // Image is taller than screen, fit by height
            newHeight = windowHeight;
            newWidth = (int)(windowHeight * imageAspect);
        }

        return GetCenteredImageRect(newWidth, newHeight, windowWidth, windowHeight);
    }
    
    public static SDL_Rect GetZoomedImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, float zoomPercent)
    {
        // Convert percentage to scale factor (100% = 1.0f)
        var scaleFactor = zoomPercent / 100f;

        var newWidth = (int)(imageWidth * scaleFactor);
        var newHeight = (int)(imageHeight * scaleFactor);

        return GetCenteredImageRect(newWidth, newHeight, windowWidth, windowHeight);
    }
}