using ValeViewer.ImageLoader;
using ValeViewer.Sdl.Enum;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private Dictionary<SDL_Scancode, Action> _scanActions = null!;

    private const int ZoomStep = 10;
    
    // panning
    private bool _isPanning;
    private int _lastMouseX, _lastMouseY;

    private void InitializeInput()
    {
        _scanActions = new Dictionary<SDL_Scancode, Action>
        {
            { SDL_Scancode.SDL_SCANCODE_ESCAPE, ExitApplication },
            { SDL_Scancode.SDL_SCANCODE_RIGHT, NextImage },
            { SDL_Scancode.SDL_SCANCODE_LEFT, PreviousImage },
            { SDL_Scancode.SDL_SCANCODE_HOME, FirstImage },
            { SDL_Scancode.SDL_SCANCODE_END, LastImage },
            { SDL_Scancode.SDL_SCANCODE_I, ToggleInfo },
            { SDL_Scancode.SDL_SCANCODE_B, ToggleBackground },
            { SDL_Scancode.SDL_SCANCODE_F, ToggleFullscreen },
            { SDL_Scancode.SDL_SCANCODE_MINUS, ZoomOut },
            { SDL_Scancode.SDL_SCANCODE_EQUALS, ZoomIn },
            { SDL_Scancode.SDL_SCANCODE_0, ToggleScale }
        };
    }

    private void NextImage()
    {
        if (DirectoryNavigator.HasNext())
        {
            DirectoryNavigator.MoveToNext();
            LoadImage();
        }
    }

    private void PreviousImage()
    {
        if (DirectoryNavigator.HasPrevious())
        {
            DirectoryNavigator.MoveToPrevious();
            LoadImage();
        }
    }

    private void FirstImage()
    {
        if (DirectoryNavigator.GetIndex().index != 1)
        {
            DirectoryNavigator.MoveToFirst();
            LoadImage();
        }
    }

    private void LastImage()
    {
        var position = DirectoryNavigator.GetIndex();
        if (position.index != position.count)
        {
            DirectoryNavigator.MoveToLast();
            LoadImage();
        }
    }

    private void ToggleInfo()
    {
        _infoMode = (InfoMode)(((int)_infoMode + 1) % 3);
    }

    private void ToggleBackground()
    {
        _backgroundMode = (BackgroundMode)(((int)_backgroundMode + 1) % 3);
    }

    private void ZoomIn()
    {
        _composite.Zoom = Math.Clamp(_composite.Zoom + ZoomStep, 10, 1000);
        _composite.ScaleMode = (_composite.Zoom == 100) ? ImageScaleMode.OriginalImageSize : ImageScaleMode.Free;
    }

    private void ZoomOut()
    {
        _composite.Zoom = Math.Clamp(_composite.Zoom - ZoomStep, 10, 1000);
        _composite.ScaleMode = (_composite.Zoom == 100) ? ImageScaleMode.OriginalImageSize : ImageScaleMode.Free;
    }

    private void ZoomAtPoint(int mouseX, int mouseY, float zoomChange)
    {
        _composite.ScaleMode = ImageScaleMode.Free;
        
        // Get current zoom level
        var oldZoom = _composite.Zoom / 100.0f;
        var newZoom = Math.Clamp(_composite.Zoom + (int)zoomChange, 10, 1000) / 100.0f;
    
        // Get renderer size
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
    
        // Compute relative position of the mouse within the image (based on original size)
        var relX = (mouseX - _offsetX - windowWidth / 2.0f) / (_composite.Width * oldZoom);
        var relY = (mouseY - _offsetY - windowHeight / 2.0f) / (_composite.Height * oldZoom);
    
        // Apply zoom
        _composite.Zoom = (int)(newZoom * 100);
    
        // Compute new rendered size based on original image dimensions
        _composite.RenderedWidth = (int)(_composite.Width * newZoom);
        _composite.RenderedHeight = (int)(_composite.Height * newZoom);
    
        // Adjust offset to maintain the zoom center at the same point
        _offsetX = mouseX - (int)(relX * _composite.Width * newZoom) - windowWidth / 2;
        _offsetY = mouseY - (int)(relY * _composite.Height * newZoom) - windowHeight / 2;
    }
    
    private void ToggleScale()
    {
        _composite.ScaleMode = _composite.ScaleMode switch
        {
            ImageScaleMode.OriginalImageSize => ImageScaleMode.FitToScreen,
            ImageScaleMode.FitToScreen => ImageScaleMode.OriginalImageSize,
            _ => CalculateInitialScale()
        };
    }
    
    #region Panning

    private void HandlePanning(int mouseX, int mouseY)
    {
        // TODO test with window scale factor
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        // Compute actual image scaling factors relative to window size
        var scaleX = (float)_composite.RenderedWidth / _composite.Width;
        var scaleY = (float)_composite.RenderedHeight / _composite.Height;

        // Include zoom factor correction
        var zoomFactor = _composite.Zoom / 100.0f;
        var scaleFactor = Math.Max(scaleX, scaleY) / zoomFactor;

        // Adjust movement delta
        var deltaX = (int)((mouseX - _lastMouseX) / scaleFactor);
        var deltaY = (int)((mouseY - _lastMouseY) / scaleFactor);

        _offsetX += deltaX;
        _offsetY += deltaY;

        // ClampImagePosition();

        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
    }
    
    private bool IsImageLargerThanWindow()
    {
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        return _composite.RenderedWidth > windowWidth || _composite.RenderedHeight > windowHeight;
    }

    private void ClampImagePosition()
    {
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        var maxX = Math.Max(0, _composite.RenderedWidth - windowWidth);
        var maxY = Math.Max(0, _composite.RenderedHeight - windowHeight);

        _offsetX = _composite.RenderedWidth <= windowWidth ? 0 : Math.Clamp(_offsetX, -maxX / 2, maxX / 2);
        _offsetY = _composite.RenderedHeight <= windowHeight ? 0 : Math.Clamp(_offsetY, -maxY / 2, maxY / 2);

        // If zooming out makes the image smaller, reset offsets
        if (_composite.RenderedWidth <= windowWidth && _composite.RenderedHeight <= windowHeight)
        {
            _offsetX = 0;
            _offsetY = 0;
        }
    }

    #endregion
}