using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private const string Title = "Vale Viewer";

    private IntPtr _window;

    private void CreateWindow()
    {
        SetWindowDimensions();
        var flags = GetWindowFlags();

        _window = SDL_CreateWindow(Title,
            SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
            _windowedWidth, _windowedHeight,
            flags);

        ValidateWindowCreation();
    }

    private void SetWindowDimensions()
    {
        SDL_GetCurrentDisplayMode(0, out var displayMode);
        _windowedWidth = displayMode.w;
        _windowedHeight = displayMode.h;
    }

    private SDL_WindowFlags GetWindowFlags()
    {
        const SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;
        return _fullscreen
            ? flags | SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP
            : flags | SDL_WindowFlags.SDL_WINDOW_SHOWN;
    }

    private void ValidateWindowCreation()
    {
        if (_window == IntPtr.Zero)
        {
            throw new Exception($"Window could not be created! SDL_Error: {SDL_GetError()}");
        }
    }
}