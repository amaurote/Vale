using System.Diagnostics;
using ValeViewer.ImageLoader;
using ValeViewer.Sdl.Enum;
using static SDL2.SDL;

namespace ValeViewer.Sdl.Core;

public partial class SdlCore
{
    private double _imageLoadTime;

    private IntPtr LoadImage(string? imagePath)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(imagePath))
            return IntPtr.Zero;

        using var imageData = ImageLoaderFactory.GetImageLoader(imagePath).LoadImage(imagePath); // todo refactor

        // Create a streaming texture
        var texture = SDL_CreateTexture(
            _renderer,
            SDL_PIXELFORMAT_ABGR8888,
            (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            imageData.Width, imageData.Height
        );

        if (texture == IntPtr.Zero)
            throw new Exception($"[ImageLoader] Failed to create SDL texture: {SDL_GetError()}");

        // Lock the texture to directly copy pixels
        if (SDL_LockTexture(texture, IntPtr.Zero, out var pixelsPtr, out var pitch) != 0)
        {
            SDL_DestroyTexture(texture);
            throw new Exception($"[ImageLoader] Failed to lock texture: {SDL_GetError()}");
        }

        // Copy pixel data to SDL texture (Unmanaged to Unmanaged)
        unsafe
        {
            Buffer.MemoryCopy((void*)imageData.PixelData, (void*)pixelsPtr, imageData.Width * imageData.Height * 4, imageData.Width * imageData.Height * 4);
        }

        SDL_UnlockTexture(texture);

        stopwatch.Stop();
        _imageLoadTime = stopwatch.ElapsedMilliseconds;

        _currentImageScaleMode = CalculateInitialScale(texture);

        return texture;
    }
    
    private ImageScaleMode CalculateInitialScale(IntPtr image)
    {
        var scale = ImageScaleMode.OriginalImageSize;
        if (image == IntPtr.Zero) 
            return scale;
        
        SDL_GetRendererOutputSize(_renderer, out var windowWidth, out var windowHeight);
        SDL_QueryTexture(image, out _, out _, out var imageWidth, out var imageHeight);
        if (imageWidth > windowWidth || imageHeight > windowHeight)
        {
            scale = ImageScaleMode.FitToScreen;
        }

        return scale;
    }
}