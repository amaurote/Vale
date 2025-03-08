using SDL2;
using ValeViewer.Loader;
using ValeViewer.Sdl.Enum;
using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private IntPtr _renderer;

    private string _rendererType = "";

    private BackgroundMode _backgroundMode = BackgroundMode.Black;
    private InfoMode _infoMode = InfoMode.Basic;
    
    private int _offsetX, _offsetY;

    #region Initialize

    private void CreateRenderer()
    {
        _renderer = SDL_CreateRenderer(_window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
        {
            throw new Exception($"[Renderer] Renderer could not be created! SDL_Error: {SDL_GetError()}");
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
            case CompositeState.ImageLoaded when _composite.Image != IntPtr.Zero:
                RenderImage();
                RenderMetadata();
                break;
            case CompositeState.Loading:
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
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        SDL_SetTextureBlendMode(_composite.Image, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        
        // Handle ScaleMode
        _composite.ScaleMode ??= CalculateInitialScale();

        _composite.Zoom = _composite.ScaleMode switch
        {
            ImageScaleMode.OriginalImageSize => 100,
            ImageScaleMode.FitToScreen => CalculateFittedImageZoom(_composite.Width, _composite.Height, windowWidth, windowHeight),
            _ => _composite.Zoom
        };

        // Create SDL_Rect and update size and offset
        var destRect = GetImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, _composite.Zoom);
        
        _composite.RenderedWidth = destRect.w;
        _composite.RenderedHeight = destRect.h;

        ClampImagePosition();
        destRect.x += _offsetX;
        destRect.y += _offsetY;

        // Render
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
        const int squareSize = 10;
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
        const double minTimeThreshold = 100.0;
        
        if (_composite.ExpectedLoadTime > minTimeThreshold)
            RenderText("[Loading...]", 0, 0, true);
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
            CompositeState.ImageLoaded => $"{_composite.Width}x{_composite.Height}",
            CompositeState.Loading => loading,
            _ => failed
        };

        var lines = new List<string>
        {
            $"[File]              {navigation.index}/{navigation.count}  |  {_composite.FileName}  |  {fileSize}",
            $"[Image Size]        {imageSize}  |  Zoom: {_composite.Zoom}%",
            // $"[Image Load Time]   Expected: {_composite.ExpectedLoadTime:F2} ms  |  Actual: {_composite.ActualLoadTime:F2} ms (background task)"
        };

        var rendererString = "";
        if (!string.IsNullOrWhiteSpace(_rendererType))
            rendererString = $"Renderer: {_rendererType}";

        var displayMode = _composite.LoadState switch
        {
            CompositeState.ImageLoaded => _composite.ScaleMode switch
            {
                ImageScaleMode.OriginalImageSize => "Original image size",
                ImageScaleMode.FitToScreen => "Fit to screen",
                _ => "Free"
            },
            CompositeState.Loading => loading,
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

    private static int CalculateFittedImageZoom(int imageWidth, int imageHeight, int windowWidth, int windowHeight)
    {
        if (imageWidth == 0 || imageHeight == 0)
            return 100;

        var scaleWidth = (float)windowWidth / imageWidth;
        var scaleHeight = (float)windowHeight / imageHeight;
        var finalScale = Math.Min(scaleWidth, scaleHeight);
        
        return (int)Math.Round(finalScale * 100f, 0);
    }

    private static SDL_Rect GetImageRect(int imageWidth, int imageHeight, int windowWidth, int windowHeight, int zoomPercent)
    {
        if (imageWidth == 0 || imageHeight == 0 || zoomPercent <= 0)
            return new SDL_Rect { x = 0, y = 0, w = 0, h = 0 };

        var newWidth = (int)(imageWidth * (zoomPercent / 100f));
        var newHeight = (int)(imageHeight * (zoomPercent / 100f));

        return CreateCenteredRect(newWidth, newHeight, windowWidth, windowHeight);
    }

    private static SDL_Rect CreateCenteredRect(int width, int height, int windowWidth, int windowHeight)
    {
        return new SDL_Rect
        {
            x = (windowWidth - width) / 2,
            y = (windowHeight - height) / 2,
            w = width,
            h = height
        };
    }

    #endregion
}