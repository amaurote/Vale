using ValeViewer.Files;
using static SDL2.SDL;

namespace ValeViewer.Sdl;

public partial class SdlCore
{
    private Dictionary<SDL_Scancode, Action> _scanActions = null!;

    private const float ZoomStep = 10.0f;

    private void InitializeInput()
    {
        _scanActions = new Dictionary<SDL_Scancode, Action>
        {
            { SDL_Scancode.SDL_SCANCODE_ESCAPE, () => _running = false },
            { SDL_Scancode.SDL_SCANCODE_RIGHT, NextImage },
            { SDL_Scancode.SDL_SCANCODE_LEFT, PreviousImage },
            { SDL_Scancode.SDL_SCANCODE_I, ToggleInfo },
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
            if (_currentImage != IntPtr.Zero)
                SDL_DestroyTexture(_currentImage);

            _currentImage = LoadImage(DirectoryNavigator.Next());
        }
    }

    private void PreviousImage()
    {
        if (DirectoryNavigator.HasPrevious())
        {
            if (_currentImage != IntPtr.Zero)
                SDL_DestroyTexture(_currentImage);

            _currentImage = LoadImage(DirectoryNavigator.Previous());
        }
    }

    private void ToggleInfo()
    {
        // TODO
    }

    private bool _fullscreen;

    private void ToggleFullscreen()
    {
        if (_fullscreen)
        {
            SDL_SetWindowFullscreen(_window, 0);
            SDL_SetWindowSize(_window, _windowedWidth, _windowedHeight);
            SDL_SetWindowPosition(_window, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
            _fullscreen = false;
        }
        else
        {
            SaveWindowSize();
            SDL_SetWindowFullscreen(_window, (uint)SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            _fullscreen = true;
        }
    }

    private int _windowedWidth;
    private int _windowedHeight;

    private void SaveWindowSize()
    {
        SDL_GetWindowSize(_window, out _windowedWidth, out _windowedHeight);
    }

    private void ZoomIn()
    {
        var newZoom = _currentZoom + ZoomStep;
        _currentZoom = Math.Clamp((int)(Math.Round(newZoom / ZoomStep) * ZoomStep), 10, 1000);
        _currentImageScaleMode = ImageScaleMode.Free;
    }

    private void ZoomOut()
    {
        var newZoom = _currentZoom - ZoomStep;
        _currentZoom = Math.Clamp((int)(Math.Round(newZoom / ZoomStep) * ZoomStep), 10, 1000);
        _currentImageScaleMode = ImageScaleMode.Free;
    }

    private void ToggleScale()
    {
        _currentImageScaleMode = _currentImageScaleMode switch
        {
            ImageScaleMode.OriginalImageSize => ImageScaleMode.FitToScreen,
            ImageScaleMode.FitToScreen => ImageScaleMode.OriginalImageSize,
            _ => CalculateInitialScale(_currentImage)
        };
    }
}