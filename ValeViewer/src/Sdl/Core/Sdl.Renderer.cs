using SDL2;
using ValeViewer.ImageLoader;
using ValeViewer.Sdl.Enum;
using ValeViewer.Sdl.Utils;
using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private IntPtr _renderer;

    private string _rendererType = "";

    private BackgroundMode _backgroundMode = BackgroundMode.Black;
    private InfoMode _infoMode = InfoMode.Basic;

    #region Initialize

    private void CreateRenderer()
    {
        _renderer = SDL_CreateRenderer(_window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
        {
            throw new Exception($"[Core] Renderer could not be created! SDL_Error: {SDL_GetError()}");
        }

        CheckRendererType();
    }

    private void CheckRendererType()
    {
        if (SDL_GetRendererInfo(_renderer, out var info) != 0)
        {
            Logger.Log($"[Renderer] Failed to get renderer info: {SDL_GetError()}", Logger.LogLevel.Error);
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

        Logger.Log($"[Renderer] Renderer Type: {_rendererType}");
    }

    #endregion

    #region Render Image

    private void Render()
    {
        RenderBackground();

        switch (_composite.LoadState)
        {
            case ImageLoadState.ImageLoaded when _composite.Image != IntPtr.Zero:
                RenderImage();
                RenderMetadata();
                break;
            case ImageLoadState.ThumbnailLoaded:
                // TODO
                break;
            case ImageLoadState.Loading:
                RenderLoadingProgress();
                break;
            default:
                RenderText("No image", 0, 0, true);
                break;
        }

        RenderStatusText();
        SDL_RenderPresent(_renderer);
    }

    private void RenderImage()
    {
        _loadingTimer.Stop();

        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        SDL_SetTextureBlendMode(_composite.Image, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        _composite.ScaleMode ??= CalculateInitialScale();

        var calculatedZoom = _composite.Zoom;
        var destRect = _composite.ScaleMode switch
        {
            ImageScaleMode.FitToScreen => SdlRectFactory.GetFittedImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, out calculatedZoom),
            ImageScaleMode.OriginalImageSize => SdlRectFactory.GetCenteredImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, out calculatedZoom),
            _ => SdlRectFactory.GetZoomedImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, _composite.Zoom)
        };

        if (_composite.Zoom != calculatedZoom)
            _composite.Zoom = calculatedZoom;

        _composite.RenderedWidth = destRect.w;
        _composite.RenderedHeight = destRect.h;

        ClampImagePosition();
        destRect.x += _offsetX;
        destRect.y += _offsetY;

        SDL_RenderCopy(_renderer, _composite.Image, IntPtr.Zero, ref destRect);
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

        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        for (var y = 0; y < windowHeight; y += squareSize)
        for (var x = 0; x < windowWidth; x += squareSize)
            if ((x / squareSize + y / squareSize) % 2 == 0)
            {
                var rect = new SDL_Rect { x = x, y = y, w = squareSize, h = squareSize };
                SDL_SetRenderDrawColor(_renderer, 240, 240, 240, 255);
                SDL_RenderFillRect(_renderer, ref rect);
            }
    }

    #endregion

    #region Render Info

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

    private void RenderStatusText()
    {
        if (_infoMode == InfoMode.None)
            return;

        var lines = GetStatusTextLines();

        var yOffset = 10;
        foreach (var line in lines)
        {
            RenderText(line, 10, yOffset);
            yOffset += 25;
        }
    }

    private List<string> GetStatusTextLines()
    {
        const string loading = "loading...";
        const string failed = "...";

        var navigation = DirectoryNavigator.GetIndex();
        var fileSize = _composite.FileSize >= 2 * 1024 * 1024
            ? $"{Math.Round((double)_composite.FileSize / (1024 * 1024), 1)} MB"
            : $"{Math.Round((double)_composite.FileSize / 1024)} kB";
        var imageSize = _composite.LoadState switch
        {
            ImageLoadState.ImageLoaded => $"{_composite.Width}x{_composite.Height}",
            ImageLoadState.Loading or ImageLoadState.ThumbnailLoaded => loading,
            _ => failed
        };

        var lines = new List<string>
        {
            $"[File]              {navigation.index}/{navigation.count}  |  {_composite.FileName}  |  {fileSize}",
            $"[Image Size]        {imageSize}  |  Zoom: {_composite.Zoom}%",
            $"[Image Load Time]   Expected: {_composite.ExpectedLoadTime:F2} ms  |  Actual: {_composite.ActualLoadTime:F2} ms"
        };

        var rendererString = "";
        if (!string.IsNullOrWhiteSpace(_rendererType))
            rendererString = $"Renderer: {_rendererType}";

        var displayMode = _composite.LoadState switch
        {
            ImageLoadState.ImageLoaded => _composite.ScaleMode switch
            {
                ImageScaleMode.OriginalImageSize => "Original image size",
                ImageScaleMode.FitToScreen => "Fit to screen",
                _ => "Free"
            },
            ImageLoadState.Loading or ImageLoadState.ThumbnailLoaded => loading,
            _ => failed
        };
        displayMode = "Display Mode: " + displayMode;

        lines.Add($"[System]            " + string.Join("  |  ", new[] { rendererString, displayMode }.Where(x => !string.IsNullOrWhiteSpace(x))));
        return lines;
    }

    private void RenderMetadata()
    {
        if (_infoMode != InfoMode.BasicAndExif)
            return;

        var lines = new List<string>();

        if (_composite.Metadata.Count == 0)
        {
            lines.Add("[No EXIF Metadata]");
        }
        else
        {
            var metadata = _composite.Metadata;

            metadata.TryGetValue("Make", out var cameraMake);
            metadata.TryGetValue("Model", out var cameraModel);
            metadata.TryGetValue("Lens", out var lensModel);
            lines.Add(string.Join("  |  ", new[] { cameraMake, cameraModel, lensModel }.Where(x => !string.IsNullOrWhiteSpace(x))));

            metadata.TryGetValue("FNumber", out var fNumber);
            metadata.TryGetValue("ExposureTime", out var exposureTime);
            metadata.TryGetValue("ISO", out var iso);

            if (!string.IsNullOrWhiteSpace(iso))
                iso = $"ISO {iso}";

            lines.Add(string.Join("  |  ", new[] { fNumber, exposureTime, iso }.Where(x => !string.IsNullOrWhiteSpace(x))));

            metadata.TryGetValue("Taken", out var taken);

            if (!string.IsNullOrWhiteSpace(taken))
                lines.Add($"Taken: {taken}");

            // todo image details like color profile and channel depth

            if (metadata.ContainsKey("GPSLatitude") && metadata.ContainsKey("GPSLongitude"))
                lines.Add("GPS Position Available");
        }

        var yOffset = 150;
        foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            RenderText(line, 10, yOffset);
            yOffset += 25;
        }
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

            SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
            SDL_Rect destRect;
            if (centered)
            {
                destRect = new SDL_Rect
                {
                    x = (windowWidth - textWidth) / 2 + x,
                    y = (windowHeight - textHeight) / 2 + y,
                    w = textWidth,
                    h = textHeight
                };
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

    #region Size & Scale

    private ImageScaleMode CalculateInitialScale()
    {
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        if (_composite.Width > windowWidth || _composite.Height > windowHeight)
        {
            return ImageScaleMode.FitToScreen;
        }

        return ImageScaleMode.OriginalImageSize;
    }

    #endregion
}