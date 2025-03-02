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
        var (windowWidth, windowHeight, scaleFactor) = GetWindowSizeAndScale();

        switch (_composite.LoadState)
        {
            case ImageLoadState.ImageLoaded when _composite.Image != IntPtr.Zero:
            {
                _loadingTimer.Stop();

                SDL_SetTextureBlendMode(_composite.Image, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                _composite.ScaleMode ??= CalculateInitialScale();

                var calculatedZoom = _composite.Zoom;
                var destRect = _composite.ScaleMode switch
                {
                    ImageScaleMode.FitToScreen => SdlRectFactory.GetFittedImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, out calculatedZoom, scaleFactor),
                    ImageScaleMode.OriginalImageSize => SdlRectFactory.GetCenteredImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, out calculatedZoom, scaleFactor),
                    _ => SdlRectFactory.GetZoomedImageRect(_composite.Width, _composite.Height, windowWidth, windowHeight, _composite.Zoom, scaleFactor)
                };

                if (_composite.Zoom != calculatedZoom)
                    _composite.Zoom = calculatedZoom;

                SDL_RenderCopy(_renderer, _composite.Image, IntPtr.Zero, ref destRect);

                RenderMetadataPanel();
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

        var (windowWidth, windowHeight, scaleFactor) = GetWindowSizeAndScale();
        var adjustedWidth = (int)(windowWidth / scaleFactor);
        var adjustedHeight = (int)(windowHeight / scaleFactor);

        for (var y = 0; y < adjustedHeight; y += squareSize)
        for (var x = 0; x < adjustedWidth; x += squareSize)
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
        if(_infoMode == InfoMode.None)
            return;
        
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

    private void RenderMetadataPanel()
    {
        if (_infoMode != InfoMode.BasicAndExif)
            return;

        var (windowWidth, windowHeight, scaleFactor) = GetWindowSizeAndScale();

        // Scale panel dimensions
        var panelWidth = (int)(600 / scaleFactor);
        var panelHeight = (int)(1000 / scaleFactor);
        var x = (int)((windowWidth - panelWidth - 20) / scaleFactor);
        var y = (int)((windowHeight - panelHeight) / 2.0 / scaleFactor);

        // Create a transparent texture for the panel
        IntPtr panelTexture = SDL_CreateTexture(_renderer, SDL_PIXELFORMAT_RGBA8888,
            (int)SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, panelWidth + 20, panelHeight);

        if (panelTexture == IntPtr.Zero)
        {
            Logger.Log("[Renderer] Failed to create texture for metadata panel.", Logger.LogLevel.Error);
            return;
        }

        SDL_SetTextureBlendMode(panelTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL_SetRenderTarget(_renderer, panelTexture);
        SDL_SetRenderDrawColor(_renderer, 50, 50, 50, 200);
        SDL_RenderClear(_renderer);
        SDL_SetRenderTarget(_renderer, IntPtr.Zero);

        SDL_Rect panelRect = new() { x = x - 10, y = y - 10, w = panelWidth + 20, h = panelHeight };
        SDL_RenderCopy(_renderer, panelTexture, IntPtr.Zero, ref panelRect);
        SDL_DestroyTexture(panelTexture);

        var textX = x;
        var textY = y + 10;
        var lineHeight = (int)(22 / scaleFactor);
        var lineSpacing = (int)(4 / scaleFactor);
        var panelHeightMargin = lineHeight;
        var maxLines = (panelHeight - panelHeightMargin) / (lineHeight + lineSpacing);
        var currentLines = 0;

        foreach (var entry in _composite.Metadata)
        {
            if (currentLines >= maxLines)
                break;

            var fullText = $"{entry.Key}: {entry.Value}";
            var wrappedLines = SdlTtfUtils.WrapText(fullText, panelWidth, _font16);

            foreach (var line in wrappedLines)
            {
                if (currentLines >= maxLines)
                    break;

                RenderText(line, textX, textY);
                textY += lineHeight + lineSpacing;
                currentLines++;
            }
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

            // Get scaled window size
            var (windowWidth, windowHeight, scaleFactor) = GetWindowSizeAndScale();
            var adjustedWidth = (int)(windowWidth / scaleFactor);
            var adjustedHeight = (int)(windowHeight / scaleFactor);

            SDL_Rect destRect;
            if (centered)
            {
                destRect = SdlRectFactory.GetCenteredImageRect(textWidth, textHeight, adjustedWidth, adjustedHeight, out _);
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

    #region Size & Scale

    private (int physicalWidth, int physicalHeight, float scaleFactor) GetWindowSizeAndScale()
    {
        SDL_GetWindowSize(_window, out var logicalWidth, out var logicalHeight);
        SDL_GetRendererOutputSize(_renderer, out var physicalWidth, out var physicalHeight);

        if (logicalWidth == 0 || logicalHeight == 0) return (physicalWidth, physicalHeight, 1.0f);

        var scaleX = (float)physicalWidth / logicalWidth;
        var scaleY = (float)physicalHeight / logicalHeight;
        var scaleFactor = Math.Max(scaleX, scaleY); // Typically the same, but just in case.

        return (physicalWidth, physicalHeight, scaleFactor);
    }

    private ImageScaleMode CalculateInitialScale()
    {
        var (windowWidth, windowHeight, scaleFactor) = GetWindowSizeAndScale();

        var adjustedWidth = (int)(windowWidth / scaleFactor);
        var adjustedHeight = (int)(windowHeight / scaleFactor);

        if (_composite.Width > adjustedWidth || _composite.Height > adjustedHeight)
        {
            return ImageScaleMode.FitToScreen;
        }

        return ImageScaleMode.OriginalImageSize;
    }

    #endregion
}