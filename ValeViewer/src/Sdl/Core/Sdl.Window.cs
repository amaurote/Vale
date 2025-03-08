using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private const string Title = "Vale Viewer";

    private IntPtr _window;

    private bool _fullscreen;
    private int _windowedWidth;
    private int _windowedHeight;
    private int _windowedX;
    private int _windowedY;

    private void CreateWindow()
    {
        SetWindowDimensions();
        
        // start always windowed
        const SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE;

        _window = SDL_CreateWindow(Title, _windowedX, _windowedY, _windowedWidth, _windowedHeight, flags);
        if (_window == IntPtr.Zero)
        {
            throw new Exception($"[Window] Window could not be created! SDL_Error: {SDL_GetError()}");
        }

        // Get display DPI scale
        var displayIndex = SDL_GetWindowDisplayIndex(_window);
        if (SDL_GetDisplayDPI(displayIndex, out var dpi, out _, out _) == 0)
        {
            var dpiScale = dpi / 96.0f; // Normalize against default 96 DPI
            Logger.Log($"[Window] DPI Scale Factor: {dpiScale}");
        }
    }

    private void SetWindowDimensions()
    {
        SDL_GetCurrentDisplayMode(0, out var displayMode);
        _windowedWidth = displayMode.w;
        _windowedHeight = displayMode.h;
        _windowedX = SDL_WINDOWPOS_CENTERED;
        _windowedY = SDL_WINDOWPOS_CENTERED;
    }

    private void ToggleFullscreen()
    {
        if (_fullscreen)
            ExitFullscreen();
        else
            EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        SaveWindowSize();
        SDL_SetWindowFullscreen(_window, (uint)SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
        SDL_SetWindowResizable(_window, SDL_bool.SDL_FALSE);
        _fullscreen = true;
    }

    private void ExitFullscreen()
    {
        SDL_SetWindowFullscreen(_window, 0);
        SDL_SetWindowPosition(_window, _windowedX, _windowedY);
        SDL_SetWindowResizable(_window, SDL_bool.SDL_TRUE);
        SDL_SetWindowSize(_window, _windowedWidth, _windowedHeight);
        _fullscreen = false;
    }

    private void SaveWindowSize()
    {
        SDL_GetWindowSize(_window, out _windowedWidth, out _windowedHeight);
        SDL_GetWindowPosition(_window, out _windowedX, out _windowedY);
    }
}