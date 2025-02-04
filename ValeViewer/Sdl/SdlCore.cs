using System.Diagnostics;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ValeViewer.Files;
using static SDL2.SDL;

namespace ValeViewer.Sdl;

public class SdlCore : IDisposable
{
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _font16;
    
    private readonly Dictionary<SDL_Keycode, Action> _keyActions;

    private IntPtr _currentImage;

    private bool _running = true;
    
    #region Initialize

    public SdlCore()
    {
        if (SDL_Init(SDL_INIT_VIDEO) < 0)
        {
            throw new Exception($"SDL could not initialize! SDL_Error: {SDL_GetError()}");
        }

        if (SDL_ttf.TTF_Init() < 0)
        {
            throw new Exception($"SDL_ttf could not initialize! SDL_Error: {SDL_GetError()}");
        }

        _keyActions = new Dictionary<SDL_Keycode, Action>
        {
            { SDL_Keycode.SDLK_ESCAPE, () => _running = false },
            { SDL_Keycode.SDLK_RIGHT, NextImage },
            { SDL_Keycode.SDLK_LEFT, PreviousImage }
        };

        CreateWindow();
        CreateRenderer();
        LoadFont();

        /*
         * Load directory here:
         * DirectoryNavigator.SearchImages(initialFilePath);
         */
        
        _currentImage = LoadImage(DirectoryNavigator.Current());
    }

    private void CreateWindow()
    {
        // WARNING: For development use SDL_WindowFlags.SDL_WINDOW_SHOWN
        _window = SDL_CreateWindow("Vale Viewer",
            SDL_WINDOWPOS_CENTERED,
            SDL_WINDOWPOS_CENTERED,
            0, 0, // Width & Height are ignored in fullscreen
            SDL_WindowFlags.SDL_WINDOW_FULLSCREEN | SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI);

        if (_window == IntPtr.Zero)
        {
            throw new Exception($"Window could not be created! SDL_Error: {SDL_GetError()}");
        }
    }

    private void CreateRenderer()
    {
        _renderer = SDL_CreateRenderer(_window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
        {
            throw new Exception($"Renderer could not be created! SDL_Error: {SDL_GetError()}");
        }

        CheckRendererType();
    }

    private string _rendererType = "";
    
    private void CheckRendererType()
    {
        if (SDL_GetRendererInfo(_renderer, out SDL_RendererInfo info) != 0)
        {
            Console.WriteLine($"Failed to get renderer info: {SDL_GetError()}");
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
    }

    private void LoadFont()
    {
        _font16 = SDL_ttf.TTF_OpenFont(TtfLoader.GetDefaultFontPath(), 16);
        if (_font16 == IntPtr.Zero)
        {
            throw new Exception($"Failed to load font: {SDL_GetError()}");
        }
    }

    #endregion

    #region Load Image

    private double _loadTime;
    
    private IntPtr LoadImage(string? imagePath)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(imagePath))
            return IntPtr.Zero;

        // Load image with ImageSharp
        using var image = Image.Load<Rgba32>(imagePath);
        var width = image.Width;
        var height = image.Height;

        // Create a streaming texture
        var texture = SDL_CreateTexture(
            _renderer,
            SDL_PIXELFORMAT_ABGR8888,
            (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            width, height
        );

        if (texture == IntPtr.Zero)
            throw new Exception($"Failed to create SDL texture: {SDL_GetError()}");

        // Lock the texture to directly copy pixels
        if (SDL_LockTexture(texture, IntPtr.Zero, out var pixelsPtr, out var pitch) != 0)
        {
            SDL_DestroyTexture(texture);
            throw new Exception($"Failed to lock texture: {SDL_GetError()}");
        }

        // Copy pixel data to the texture buffer
        unsafe
        {
            var pixels = (byte*)pixelsPtr;
            image.CopyPixelDataTo(new Span<byte>(pixels, width * height * 4));
        }

        SDL_UnlockTexture(texture);

        stopwatch.Stop();
        _loadTime = stopwatch.ElapsedMilliseconds;

        return texture;
    }

    private ImageScale CalculateInitialScale(IntPtr image)
    {
        var scale = ImageScale.OriginalImageSize;
        if (image == IntPtr.Zero) 
            return scale;
        
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        SDL_QueryTexture(image, out _, out _, out var imageWidth, out var imageHeight);
        if (imageWidth > windowWidth || imageHeight > windowHeight)
        {
            scale = ImageScale.FitToScreen;
        }

        return scale;
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
            if (e.type == SDL_EventType.SDL_QUIT) _running = false;
            if (e.type == SDL_EventType.SDL_KEYDOWN && _keyActions.TryGetValue(e.key.keysym.sym, out var action))
            {
                action.Invoke();
            }
        }
    }
    
    private void Render()
    {
        SDL_RenderClear(_renderer);
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        if (_currentImage != IntPtr.Zero)
        {
            SDL_QueryTexture(_currentImage, out _, out _, out var imageWidth, out var imageHeight);
            var destRect = CalculateInitialScale(_currentImage) switch
            {
                ImageScale.FitToScreen => SdlRectFactory.GetFittedImageRect(imageWidth, imageHeight, windowWidth, windowHeight),
                _ => SdlRectFactory.GetCenteredImageRect(imageWidth, imageHeight, windowWidth, windowHeight)
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

    private void RenderStatusText()
    {
        var fileName = Path.GetFileName(DirectoryNavigator.Current());
        var navResponse = DirectoryNavigator.GetCounts();
        var status = $"{navResponse.Index}/{navResponse.Count}  |  {fileName}  |  Image Load Time: {_loadTime:F2} ms";

        if (!string.IsNullOrWhiteSpace(_rendererType))
            status += $"  |  Renderer: {_rendererType}";
        
        RenderText(status, 10, 10);
    }

    private void RenderText(string text, int x, int y)
    {
        if (string.IsNullOrWhiteSpace(text) || _font16 == IntPtr.Zero)
            return;

        var white = new SDL_Color { r = 255, g = 255, b = 255, a = 255 };

        var surface = SDL_ttf.TTF_RenderText_Blended(_font16, text, white);
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
        var destRect = SdlRectFactory.GetCenteredImageRect(textWidth, textHeight, screenWidth, screenHeight);

        SDL_RenderCopy(_renderer, texture, IntPtr.Zero, ref destRect);
        SDL_DestroyTexture(texture);
    }

    #endregion

    public void Dispose()
    {
        if (_currentImage != IntPtr.Zero) SDL_DestroyTexture(_currentImage);
        if (_font16 != IntPtr.Zero) SDL_ttf.TTF_CloseFont(_font16);
        
        SDL_DestroyRenderer(_renderer);
        SDL_DestroyWindow(_window);
        SDL_Quit();

        GC.SuppressFinalize(this);
    }
}