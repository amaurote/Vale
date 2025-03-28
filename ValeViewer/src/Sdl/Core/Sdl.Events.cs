using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private void HandleEvents()
    {
        while (SDL_PollEvent(out var e) != 0)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_KEYDOWN
                    when e.key.repeat == 0 && _scanActions.TryGetValue(e.key.keysym.scancode, out var scanAction):
                    scanAction.Invoke();
                    break;

                case SDL_EventType.SDL_DROPBEGIN:
                    Logger.Log("[Events] File drop started.");
                    break;

                case SDL_EventType.SDL_DROPFILE:
                    OnDropFile(e);
                    break;

                case SDL_EventType.SDL_DROPCOMPLETE:
                    Logger.Log("[Events] File drop completed.");
                    break;

                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    OnMouseButtonDown(e);
                    break;

                case SDL_EventType.SDL_MOUSEBUTTONUP:
                    OnMouseButtonUp(e);
                    break;

                case SDL_EventType.SDL_MOUSEMOTION:
                    OnMouseMotion(e);
                    break;

                case SDL_EventType.SDL_MOUSEWHEEL:
                    OnMouseWheel(e);
                    break;

                case SDL_EventType.SDL_WINDOWEVENT:
                    UpdateScaleFactors();
                    break;

                case SDL_EventType.SDL_QUIT:
                    ExitApplication();
                    break;
            }
        }
    }

    private void OnMouseButtonDown(SDL_Event e)
    {
        if (e.button.button == SDL_BUTTON_LEFT)
        {
            StartPanning(e.motion.x, e.motion.y);
        }
    }

    private void OnMouseButtonUp(SDL_Event e)
    {
        if (e.button.button == SDL_BUTTON_LEFT)
        {
            StopPanning();
        }
    }

    private void OnMouseMotion(SDL_Event e)
    {
        if (_isPanning)
        {
            HandlePanning(e.motion.x, e.motion.y);
        }
    }

    private void OnMouseWheel(SDL_Event e)
    {
        SDL_GetMouseState(out var mouseX, out var mouseY);
        ZoomAtPoint(mouseX, mouseY, e.wheel.y);
    }
}