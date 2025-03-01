using ValeViewer.Sdl.Enum;
using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private Dictionary<SDL_Scancode, Action> _scanActions = null!;

    private const float ZoomStep = 10.0f;

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
            LoadImage(DirectoryNavigator.Next());
        }
    }

    private void PreviousImage()
    {
        if (DirectoryNavigator.HasPrevious())
        {
            LoadImage(DirectoryNavigator.Previous());
        }
    }

    private void FirstImage()
    {
        if (DirectoryNavigator.GetIndex().index != 1)
        {
            LoadImage(DirectoryNavigator.First());
        }
    }

    private void LastImage()
    {
        var position = DirectoryNavigator.GetIndex();
        if (position.index != position.count)
        {
            LoadImage(DirectoryNavigator.Last());
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
        _composite.Zoom = Math.Clamp(_composite.Zoom + (int)ZoomStep, 10, 1000);
        _composite.ScaleMode = (_composite.Zoom == 100) ? ImageScaleMode.OriginalImageSize : ImageScaleMode.Free;
    }

    private void ZoomOut()
    {
        var newZoom = _composite.Zoom - ZoomStep;
        _composite.Zoom = Math.Clamp((int)(Math.Round(newZoom / ZoomStep) * ZoomStep), 10, 1000);
        _composite.ScaleMode = (_composite.Zoom == 100) ? ImageScaleMode.OriginalImageSize : ImageScaleMode.Free;
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
}