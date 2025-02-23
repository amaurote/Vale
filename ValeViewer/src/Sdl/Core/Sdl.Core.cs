using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using ValeViewer.ImageLoader;
using ValeViewer.Sdl.Enum;
using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore : IDisposable
{
    private IntPtr _renderer;
    private IntPtr _font16;

    private readonly ImageComposite _composite = new();

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

        LoadImage(DirectoryNavigator.Current());
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
        _font16 = SDL_ttf.TTF_OpenFont(TtfLoader.GetMonospaceFontPath(), 16);
        if (_font16 == IntPtr.Zero)
        {
            throw new Exception($"[Core] Failed to load font: {SDL_GetError()}");
        }
    }

    private readonly Stopwatch _loadingTimer = new();

    private void LoadImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        _loadingTimer.Restart();

        _ = _composite.LoadImageAsync(imagePath, _renderer);
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
                        LoadImage(DirectoryNavigator.Current());
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

    private void Render()
    {
        RenderBackground();
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        switch (_composite.LoadState)
        {
            case ImageLoadState.ImageLoaded when _composite.Image != IntPtr.Zero:
            {
                _loadingTimer.Stop();

                SDL_SetTextureBlendMode(_composite.Image, SDL_BlendMode.SDL_BLENDMODE_BLEND);

                var calculatedZoom = _composite.Zoom;
                var destRect = _composite.ScaleMode switch
                {
                    ImageScaleMode.FitToScreen => SdlRectFactory.GetFittedImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, out calculatedZoom),
                    ImageScaleMode.OriginalImageSize => SdlRectFactory.GetCenteredImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, out calculatedZoom),
                    _ => SdlRectFactory.GetZoomedImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, _composite.Zoom)
                };

                if (_composite.Zoom != calculatedZoom)
                    _composite.Zoom = calculatedZoom;

                SDL_RenderCopy(_renderer, _composite.Image, IntPtr.Zero, ref destRect);

                RenderStatusText();
                break;
            }
            case ImageLoadState.ThumbnailLoaded:
                // TODO
                SDL_RenderCopy(_renderer, _composite.Thumbnail, IntPtr.Zero, IntPtr.Zero);
                RenderLoadingProgress();
                break;
            case ImageLoadState.Loading:
                RenderLoadingProgress();
                break;
            case ImageLoadState.Failed:
            case ImageLoadState.NoImage:
            default:
                RenderText("No image", 0, 0, true);
                break;
        }

        RenderStatusText();
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
            if ((x / squareSize + y / squareSize) % 2 == 0)
            {
                var rect = new SDL_Rect { x = x, y = y, w = squareSize, h = squareSize };
                SDL_SetRenderDrawColor(_renderer, 240, 240, 240, 255);
                SDL_RenderFillRect(_renderer, ref rect);
            }
    }

    private void RenderStatusText()
    {
        var navigation = DirectoryNavigator.GetIndex();
        var fileSize = _composite.FileSize >= 2 * 1024 * 1024
            ? $"{Math.Round((double)_composite.FileSize / (1024 * 1024), 1)} MB"
            : $"{Math.Round((double)_composite.FileSize / 1024)} kB";
        var imageSize = _composite.LoadState switch
        {
            ImageLoadState.ImageLoaded => $"{_composite.Width}x{_composite.Height}",
            ImageLoadState.Loading or ImageLoadState.ThumbnailLoaded => "loading...",
            _ => "???"
        };

        var lines = new List<string>
        {
            $"[File]              {navigation.index}/{navigation.count}  |  {_composite.FileName}  |  {fileSize}",
            $"[Image Size]        {imageSize}  |  Zoom: {_composite.Zoom}%",
            $"[Image Load Time]   Expected: {_composite.ExpectedLoadTime:F2} ms  |  Actual: {_composite.ActualLoadTime:F2} ms"
        };

        if (!string.IsNullOrWhiteSpace(_rendererType))
            lines.Add($"[System]            Renderer: {_rendererType}");

        var yOffset = 10;
        foreach (var line in lines)
        {
            RenderText(line, 10, yOffset);
            yOffset += 25;
        }
    }

    private void RenderLoadingProgress()
    {
        const int maxDots = 10;
        const double minTimeThreshold = 100.0;
        const double barTimeThreshold = 200.0;

        if (_composite.ExpectedLoadTime < barTimeThreshold)
        {
            if (_composite.ExpectedLoadTime > minTimeThreshold)
                RenderText("[Loading...]", 0, 0, true);

            return;
        }

        // Get elapsed time since loading started
        double elapsed = _loadingTimer.ElapsedMilliseconds;
        var progressPercentage = elapsed / _composite.ExpectedLoadTime;

        // Calculate number of dots based on progress
        var filledDots = (int)Math.Clamp(progressPercentage * maxDots + 1, 0, maxDots);
        var progressBar = new string('.', filledDots).PadRight(maxDots, ' ');

        RenderText("[Loading...]", 0, 0, true);
        RenderText($"[{progressBar}]", 0, 25, true);
    }

    private void RenderText(string text, int x, int y, bool centered = false)
    {
        if (string.IsNullOrWhiteSpace(text) || _font16 == IntPtr.Zero)
            return;

        var color = _backgroundMode == BackgroundMode.Black
            ? new SDL_Color { r = 255, g = 255, b = 255, a = 255 }
            : new SDL_Color { r = 0, g = 0, b = 0, a = 255 };

        var surface = SDL_ttf.TTF_RenderText_Blended(_font16, text, color);
        if (surface == IntPtr.Zero)
            return;

        try
        {
            var texture = SDL_CreateTextureFromSurface(_renderer, surface);
            if (texture == IntPtr.Zero) return;

            SDL_QueryTexture(texture, out _, out _, out var textWidth, out var textHeight);
            SDL_GetRendererOutputSize(_renderer, out var screenWidth, out var screenHeight);

            SDL_Rect destRect;
            if (centered)
            {
                destRect = SdlRectFactory.GetCenteredImageRect(textWidth, textHeight, screenWidth, screenHeight, out _);
                destRect.x += x;
                destRect.y += y;
            }
            else
            {
                destRect = new SDL_Rect { x = x, y = y, w = textWidth, h = textHeight };
            }

            SDL_RenderCopy(_renderer, texture, IntPtr.Zero, ref destRect);
            SDL_DestroyTexture(texture);
        }
        finally
        {
            SDL_FreeSurface(surface);
        }
    }

    #endregion

    private ImageScaleMode CalculateInitialScale(IntPtr image)
    {
        var scale = ImageScaleMode.OriginalImageSize;
        if (image == IntPtr.Zero)
            return scale;

        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        if (_composite.Width > windowWidth || _composite.Height > windowHeight)
        {
            scale = ImageScaleMode.FitToScreen;
        }

        return scale;
    }

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
        LoadTimeEstimator.SaveTimeDataToFile();
        _running = false;
    }

    public void Dispose()
    {
        Logger.Log("[Core] Disposing...");

        _composite.Dispose();

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