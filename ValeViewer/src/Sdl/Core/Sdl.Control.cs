using ValeViewer.Loader;
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

    // panning and mouse zooming
    private float _scaleX, _scaleY;

    private void InitializeControl()
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

    #region Panning & Zoom-at-point

    private void ZoomAtPoint(int mouseX, int mouseY, float direction)
    {
        var zoomChange = (direction > 0) ? ZoomStep : -ZoomStep;
        
        _composite.ScaleMode = ImageScaleMode.Free;

        var oldZoom = _composite.Zoom / 100.0f;
        var newZoom = Math.Clamp(_composite.Zoom + zoomChange, 10, 1000) / 100.0f;

        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        var adjustedMouseX = mouseX * _scaleX;
        var adjustedMouseY = mouseY * _scaleY;

        // Compute relative position of the mouse within the image
        var relX = (adjustedMouseX - _offsetX - windowWidth / 2.0f) / (_composite.Width * oldZoom);
        var relY = (adjustedMouseY - _offsetY - windowHeight / 2.0f) / (_composite.Height * oldZoom);

        // Apply zoom
        _composite.Zoom = (int)(newZoom * 100);

        // Compute new rendered size based on original image dimensions
        _composite.RenderedWidth = (int)(_composite.Width * newZoom);
        _composite.RenderedHeight = (int)(_composite.Height * newZoom);

        // Adjust offset to maintain zoom center at the same point
        _offsetX = (int)(adjustedMouseX - (relX * _composite.Width * newZoom) - windowWidth / 2.0f);
        _offsetY = (int)(adjustedMouseY - (relY * _composite.Height * newZoom) - windowHeight / 2.0f);
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

    private void StartPanning(int mouseX, int mouseY)
    {
        if (!IsImageLargerThanWindow())
            return;

        SDL_SetCursor(_handCursor);
        
        // Store correctly scaled initial mouse position
        _lastMouseX = (int)(mouseX * _scaleX);
        _lastMouseY = (int)(mouseY * _scaleY);

        _isPanning = true;
    }

    private void HandlePanning(int mouseX, int mouseY)
    {
        if (!_isPanning)
            return;
        
        // Adjust mouse coordinates for screen scaling
        var adjustedMouseX = mouseX * _scaleX;
        var adjustedMouseY = mouseY * _scaleY;

        // Compute actual image scaling factors relative to window size
        var scaleFactorX = (float)_composite.RenderedWidth / _composite.Width;
        var scaleFactorY = (float)_composite.RenderedHeight / _composite.Height;
        var zoomFactor = _composite.Zoom / 100.0f;
        var scaleFactor = Math.Max(scaleFactorX, scaleFactorY) / zoomFactor;

        // Adjust movement delta
        var deltaX = (int)((adjustedMouseX - _lastMouseX) / scaleFactor);
        var deltaY = (int)((adjustedMouseY - _lastMouseY) / scaleFactor);

        _offsetX += deltaX;
        _offsetY += deltaY;

        _lastMouseX = (int)adjustedMouseX;
        _lastMouseY = (int)adjustedMouseY;
    }

    private void StopPanning()
    {
        _isPanning = false;
        SDL_SetCursor(_defaultCursor);
    }

    private bool IsImageLargerThanWindow()
    {
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        return _composite.RenderedWidth > windowWidth || _composite.RenderedHeight > windowHeight;
    }

    private void UpdateScaleFactors()
    {
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        SDL_GetWindowSize(_window, out var logicalWidth, out var logicalHeight);

        _scaleX = (float)windowWidth / logicalWidth;
        _scaleY = (float)windowHeight / logicalHeight;
    }

    #endregion
}