using System.Runtime.InteropServices;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static SDL2.SDL;

namespace ValeViewer.Sdl;

public class SdlCore : IDisposable
{
    private IntPtr _window;
    private IntPtr _renderer;
    private IntPtr _font = IntPtr.Zero;

    private IntPtr _image = IntPtr.Zero;
    private IntPtr _text = IntPtr.Zero;

    private bool _running = true;

    private ImageScale _initialScale = ImageScale.OriginalImageSize;

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

        CreateWindow();
        CreateRenderer();

        /*
         * LoadImage(...) here
         */
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
        _renderer = SDL_CreateRenderer(_window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
        if (_renderer == IntPtr.Zero)
        {
            throw new Exception($"Renderer could not be created! SDL_Error: {SDL_GetError()}");
        }
    }

    #endregion

    #region Load Image

    private void LoadImage(string path)
    {
        // Load image with ImageSharp
        using var image = Image.Load<Rgba32>(path);
        var width = image.Width;
        var height = image.Height;
        var pixels = new byte[width * height * 4]; // RGBA format (4 bytes per pixel)

        // Copy pixel data to the byte array
        image.CopyPixelDataTo(pixels);

        // Pin the byte array in memory
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        var pixelPtr = handle.AddrOfPinnedObject();

        // Create SDL surface from the pixel data
        var surface = SDL_CreateRGBSurfaceWithFormatFrom(
            pixelPtr,
            width, height,
            32, width * 4, // 4 bytes per pixel (RGBA)
            SDL_PIXELFORMAT_ABGR8888 // Correct format for SDL
        );

        // Unpin memory after SDL is done using it
        handle.Free();

        if (surface == IntPtr.Zero)
        {
            throw new Exception($"Failed to create SDL surface: {SDL_GetError()}");
        }

        // Convert surface to texture
        var texture = SDL_CreateTextureFromSurface(_renderer, surface);
        SDL_FreeSurface(surface); // Free the surface now that we have the texture

        if (texture == IntPtr.Zero)
        {
            throw new Exception($"Failed to create texture from surface: {SDL_GetError()}");
        }

        _image = texture;
        CalculateInitialScale();
    }

    private void CalculateInitialScale()
    {
        if (_image != IntPtr.Zero)
        {
            // Get window size
            SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

            // Get image size
            SDL_QueryTexture(_image, out _, out _, out var imageWidth, out var imageHeight);

            if (imageWidth <= windowWidth && imageHeight <= windowHeight)
            {
                _initialScale = ImageScale.OriginalImageSize;
                return;
            }

            _initialScale = ImageScale.FitToScreen;
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
        while (SDL_PollEvent(out SDL_Event e) != 0)
        {
            if (e.type == SDL_EventType.SDL_QUIT ||
                (e.type == SDL_EventType.SDL_KEYDOWN && e.key.keysym.sym == SDL_Keycode.SDLK_ESCAPE))
            {
                _running = false;
            }
        }
    }

    private void Render()
    {
        SDL_RenderClear(_renderer);

        // Get window size
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);

        if (_image != IntPtr.Zero)
        {
            // Get image size
            SDL_QueryTexture(_image, out _, out _, out var imageWidth, out var imageHeight);

            // Calculate image rectangle
            var destRect = _initialScale switch
            {
                ImageScale.FitToScreen => SdlRectFactory.GetFittedImageRect(imageWidth, imageHeight, windowWidth, windowHeight),
                _ => SdlRectFactory.GetCenteredImageRect(imageWidth, imageHeight, windowWidth, windowHeight)
            };

            SDL_RenderCopy(_renderer, _image, IntPtr.Zero, ref destRect);
        }
        else
        {
            CreateNoImageLabel();

            // Get text size
            SDL_QueryTexture(_text, out _, out _, out var textWidth, out var textHeight);

            // Center the text
            var textRect = SdlRectFactory.GetCenteredImageRect(textWidth, textHeight, windowWidth, windowHeight);
            SDL_RenderCopy(_renderer, _text, IntPtr.Zero, ref textRect);
        }

        SDL_RenderPresent(_renderer);
    }

    private void CreateNoImageLabel()
    {
        _font = SDL_ttf.TTF_OpenFont(TtfLoader.GetDefaultFontPath(), 24);

        if (_font == IntPtr.Zero)
            throw new Exception($"Failed to load font: {SDL_GetError()}");

        var white = new SDL_Color { r = 255, g = 255, b = 255, a = 255 };
        var surface = SDL_ttf.TTF_RenderText_Blended(_font, "No Image", white);

        if (surface == IntPtr.Zero)
            throw new Exception($"Failed to render text: {SDL_GetError()}");

        _text = SDL_CreateTextureFromSurface(_renderer, surface);
        SDL_FreeSurface(surface);

        if (_text == IntPtr.Zero)
            throw new Exception($"Failed to create texture from text: {SDL_GetError()}");
    }

    #endregion

    public void Dispose()
    {
        SDL_DestroyRenderer(_renderer);
        SDL_DestroyWindow(_window);
        SDL_Quit();

        GC.SuppressFinalize(this);
    }
}