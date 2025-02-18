using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using ValeViewer.Files;
using ValeViewer.Sdl.Enum;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore : IDisposable
{
    private IntPtr _renderer;
    private IntPtr _font16;
    
    private IntPtr _currentImage;
    private ImageScaleMode _currentImageScaleMode;
    private int _currentZoom = 100;
    
    private BackgroundMode _backgroundMode = BackgroundMode.Black;

    private bool _running = true;
    
    #region Initialize

    public SdlCore(string? imagePath, bool startInFullscreen)
    {
        _fullscreen = startInFullscreen;
        
        var stopwatch = Stopwatch.StartNew();
        
        if (SDL_Init(SDL_INIT_VIDEO) < 0)
        {
            throw new Exception($"[Core] SDL could not initialize! SDL_Error: {SDL_GetError()}");
        }
        Logger.Log("[Core] SDL Initialized");
        
        if (SDL_ttf.TTF_Init() < 0)
        {
            throw new Exception($"SDL_ttf could not initialize! SDL_Error: {SDL_GetError()}");
        }
        Logger.Log("[Core] SDL_ttf Initialized");
        
        InitializeInput();
        CreateWindow();
        CreateRenderer();
        LoadFont();
        
        Logger.Log($"[Core] Startup time: {stopwatch.ElapsedMilliseconds} ms");
        
        if (imagePath != null) 
            DirectoryNavigator.SearchImages(imagePath);
        
        _currentImage = LoadImage(DirectoryNavigator.Current());
    }

    private void CreateRenderer()
    {
        _renderer = SDL_CreateRenderer(_window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
        {
            throw new Exception($"[Core] Renderer could not be created! SDL_Error: {SDL_GetError()}");
        }

        CheckRendererType();
    }

    private string _rendererType = "";
    
    private void CheckRendererType()
    {
        if (SDL_GetRendererInfo(_renderer, out var info) != 0)
        {
            Logger.Log($"[Core] Failed to get renderer info: {SDL_GetError()}", Logger.LogLevel.Error);
            return;
        }

        if ((info.flags & (uint)SDL_RendererFlags.SDL_RENDERER_ACCELERATED) != 0)
        {
            _rendererType = "GPU accelerated";
        }
        else if ((info.flags & (uint)SDL_RendererFlags.SDL_RENDERER_SOFTWARE) != 0)
        {
            _rendererType = "Software";
        }
        
        Logger.Log($"[Core] Renderer Type: {_rendererType}");
    }

    private void LoadFont()
    {
        _font16 = SDL_ttf.TTF_OpenFont(TtfLoader.GetDefaultFontPath(), 16);
        if (_font16 == IntPtr.Zero)
        {
            throw new Exception($"[Core] Failed to load font: {SDL_GetError()}");
        }
    }

    #endregion
    
    #region Main Loop

    public void Run()
    {
        while (_running)
        {
            HandleEvents();
            Render();
        }
    }

    private void HandleEvents()
    {
        while (SDL_PollEvent(out var e) != 0)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_KEYDOWN when _scanActions.TryGetValue(e.key.keysym.scancode, out var scanAction):
                    scanAction.Invoke();
                    break;

                case SDL_EventType.SDL_DROPBEGIN:
                    Logger.Log("[Core] File drop started.");
                    break;

                case SDL_EventType.SDL_DROPFILE:
                    var droppedFile = Marshal.PtrToStringUTF8(e.drop.file);
                    if (!string.IsNullOrEmpty(droppedFile))
                    {
                        Logger.Log($"[Core] File dropped: {droppedFile}");
                        DirectoryNavigator.SearchImages(droppedFile);
                        _currentImage = LoadImage(DirectoryNavigator.Current());
                    }
                    else
                    {
                        Logger.Log("[Core] File drop failed.", Logger.LogLevel.Warn);
                    }

                    SDL_free(e.drop.file);
                    break;

                case SDL_EventType.SDL_DROPCOMPLETE:
                    Logger.Log("[Core] File drop completed.");
                    break;

                case SDL_EventType.SDL_QUIT:
                    ExitApplication();
                    break;
            }
        }
    }

    private int _currentImageWidth;
    private int _currentImageHeight;
    
    private void Render()
    {
        RenderBackground();
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        if (_currentImage != IntPtr.Zero)
        {
            SDL_SetTextureBlendMode(_currentImage, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_QueryTexture(_currentImage, out _, out _, out _currentImageWidth, out _currentImageHeight);
            var destRect = _currentImageScaleMode switch
            {
                ImageScaleMode.FitToScreen => SdlRectFactory.GetFittedImageRect(_currentImageWidth, _currentImageHeight, windowWidth, windowHeight, out _currentZoom),
                ImageScaleMode.OriginalImageSize => SdlRectFactory.GetCenteredImageRect(_currentImageWidth, _currentImageHeight, windowWidth, windowHeight, out _currentZoom),
                _ => SdlRectFactory.GetZoomedImageRect(_currentImageWidth, _currentImageHeight, windowWidth, windowHeight, _currentZoom)
            };
            SDL_RenderCopy(_renderer, _currentImage, IntPtr.Zero, ref destRect);

            RenderStatusText();
        }
        else
        {
            RenderCenteredText("No image");
        }

        SDL_RenderPresent(_renderer);
    }

    private void RenderBackground()
    {
        switch (_backgroundMode)
        {
            case BackgroundMode.White:
                SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                SDL_RenderClear(_renderer);
                break;
            case BackgroundMode.Checkerboard:
                SDL_RenderClear(_renderer);
                RenderCheckerboard();
                break;
            case BackgroundMode.Black:
            default:
                SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
                SDL_RenderClear(_renderer);
                break;
        }
    }

    private void RenderCheckerboard()
    {
        const int squareSize = 50;
        SDL_SetRenderDrawColor(_renderer, 200, 200, 200, 255);
        SDL_RenderClear(_renderer);
        SDL_GetRendererOutputSize(_renderer, out var width, out var height);

        for (var y = 0; y < height; y += squareSize)
        for (var x = 0; x < width; x += squareSize)
        {
            if ((x / squareSize + y / squareSize) % 2 == 0)
            {
                var rect = new SDL_Rect { x = x, y = y, w = squareSize, h = squareSize };
                SDL_SetRenderDrawColor(_renderer, 240, 240, 240, 255);
                SDL_RenderFillRect(_renderer, ref rect);
            }
        }
    }

    private void RenderStatusText()
    {
        var fileName = Path.GetFileName(DirectoryNavigator.Current());
        var navigation = DirectoryNavigator.GetIndex();
        
        RenderText($"{navigation.index}/{navigation.count}  |  {fileName}  |  " +
                   $"{_currentImageWidth}x{_currentImageHeight}  |  Zoom: {_currentZoom}%", 10, 10);
        RenderText($"Image Load Time: {_imageLoadTime:F2} ms", 10, 35);
        
        if (!string.IsNullOrWhiteSpace(_rendererType))
        {
            RenderText($"Renderer: {_rendererType}", 10, 60);
        }
    }

    private void RenderText(string text, int x, int y)
    {
        if (string.IsNullOrWhiteSpace(text) || _font16 == IntPtr.Zero)
            return;

        var color = _backgroundMode == BackgroundMode.Black 
            ? new SDL_Color { r = 255, g = 255, b = 255, a = 255 } 
            : new SDL_Color { r = 0, g = 0, b = 0, a = 255 };

        var surface = SDL_ttf.TTF_RenderText_Blended(_font16, text, color);
        if (surface == IntPtr.Zero)
            return;

        var texture = SDL_CreateTextureFromSurface(_renderer, surface);
        SDL_FreeSurface(surface);
        if (texture == IntPtr.Zero)
            return;

        SDL_QueryTexture(texture, out _, out _, out var textWidth, out var textHeight);
        var destRect = new SDL_Rect { x = x, y = y, w = textWidth, h = textHeight };
        
        SDL_RenderCopy(_renderer, texture, IntPtr.Zero, ref destRect);
        SDL_DestroyTexture(texture);
    }

    private void RenderCenteredText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _font16 == IntPtr.Zero) 
            return;

        var white = new SDL_Color { r = 255, g = 255, b = 255, a = 255 };

        var surface = SDL_ttf.TTF_RenderText_Blended(_font16, text, white);
        if (surface == IntPtr.Zero) return;

        var texture = SDL_CreateTextureFromSurface(_renderer, surface);
        SDL_FreeSurface(surface);
        if (texture == IntPtr.Zero)
            return;

        SDL_QueryTexture(texture, out _, out _, out var textWidth, out var textHeight);
        SDL_GetRendererOutputSize(_renderer, out var screenWidth, out var screenHeight);
        var destRect = SdlRectFactory.GetCenteredImageRect(textWidth, textHeight, screenWidth, screenHeight, out _);

        SDL_RenderCopy(_renderer, texture, IntPtr.Zero, ref destRect);
        SDL_DestroyTexture(texture);
    }

    #endregion

    private delegate void SdlFreeDelegate(IntPtr mem);
    private static readonly SdlFreeDelegate SDL_free = LoadSdlFunction<SdlFreeDelegate>("SDL_free");

    private static TDelegate LoadSdlFunction<TDelegate>(string functionName) where TDelegate : Delegate
    {
        var libHandle = NativeLibraryLoader.Resolve("SDL2");
        var functionPtr = NativeLibrary.GetExport(libHandle, functionName);
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPtr);
    }

    private void ExitApplication()
    {
        _running = false;
    }

    public void Dispose()
    {
        Logger.Log("[Core] Disposing...");

        if (_currentImage != IntPtr.Zero)
            SDL_DestroyTexture(_currentImage);
        if (_font16 != IntPtr.Zero)
            SDL_ttf.TTF_CloseFont(_font16);
        if (_renderer != IntPtr.Zero)
            SDL_DestroyRenderer(_renderer);
        if (_window != IntPtr.Zero)
            SDL_DestroyWindow(_window);
        
        SDL_Quit();
        GC.SuppressFinalize(this);
    }
}