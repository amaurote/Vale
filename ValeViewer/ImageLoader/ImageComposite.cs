using System.Diagnostics;
using ValeViewer.ImageDecoder;
using ValeViewer.Sdl.Enum;
using static SDL2.SDL;

namespace ValeViewer.ImageLoader;

public class ImageComposite : IDisposable
{
    public IntPtr Image { get; private set; } = IntPtr.Zero;
    public IntPtr Thumbnail { get; private set; } = IntPtr.Zero;

    public double ExpectedLoadTime { get; private set; }
    public double ActualLoadTime { get; private set; }

    public int Zoom { get; set; } = 100;
    public ImageScaleMode ScaleMode { get; set; } = ImageScaleMode.OriginalImageSize;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }

    public ImageLoadState LoadState { get; private set; } = ImageLoadState.NoImage;

    public void LoadImage(string imagePath, IntPtr renderer)
    {
        Dispose();
        
        LoadState = ImageLoadState.Loading;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            FileSize = new FileInfo(imagePath).Length;
            FileName = Path.GetFileName(imagePath);

            using var imageData = ImageLoaderFactory.GetImageDecoder(imagePath).Decode(imagePath);
            Image = SDL_CreateTexture(
                renderer,
                SDL_PIXELFORMAT_ABGR8888,
                (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                imageData.Width, imageData.Height
            );
            
            if (Image == IntPtr.Zero)
                throw new Exception($"[ImageComposite] Failed to create SDL texture: {SDL_GetError()}");

            if (SDL_LockTexture(Image, IntPtr.Zero, out var pixelsPtr, out var pitch) != 0)
            {
                SDL_DestroyTexture(Image);
                throw new Exception($"[ImageComposite] Failed to lock texture: {SDL_GetError()}");
            }

            unsafe
            {
                long bufferSize = imageData.Width * imageData.Height * 4;
                long pitchSize = pitch * imageData.Height;

                if (bufferSize > pitchSize)
                    throw new Exception("[ImageComposite] Pixel data size exceeds allocated texture size!");

                Buffer.MemoryCopy((void*)imageData.PixelData, (void*)pixelsPtr, pitchSize, bufferSize);
            }

            SDL_UnlockTexture(Image);
            

            Width = imageData.Width;
            Height = imageData.Height;

            SDL_GetRendererOutputSize(renderer, out var windowWidth, out var windowHeight);
            ScaleMode = (Width > windowWidth || Height > windowHeight) ? ImageScaleMode.FitToScreen : ImageScaleMode.OriginalImageSize;

            LoadState = ImageLoadState.ImageLoaded;
        }
        catch (ImageDecodeException ex)
        {
            LoadState = ImageLoadState.Failed;
            Logger.Log(ex.Message, Logger.LogLevel.Warn);
        }
        catch (FileNotFoundException)
        {
            LoadState = ImageLoadState.Failed;
            Logger.Log($"[ImageComposite] Failed to load image: {imagePath}", Logger.LogLevel.Error);
        }
        catch (Exception ex)
        {
            LoadState = ImageLoadState.Failed;
            Logger.Log($"[ImageComposite] Failed to load image: {ex.Message}", Logger.LogLevel.Error);
        }
        finally
        {
            stopwatch.Stop();
            ActualLoadTime = stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    public async Task LoadImageAsync(string imagePath, IntPtr renderer)
    {
        throw new NotImplementedException();
    }


    private async Task LoadThumbnailAsync(string imagePath, IntPtr renderer, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    private async Task EstimateLoadTimeAsync()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        ExpectedLoadTime = 0;
        ActualLoadTime = 0;
        Zoom = 100;
        ScaleMode = ImageScaleMode.OriginalImageSize;
        Width = 0;
        Height = 0;
        FileName = string.Empty;
        FileSize = 0;

        if (Image != IntPtr.Zero)
        {
            SDL_DestroyTexture(Image);
        }

        if (Thumbnail != IntPtr.Zero)
        {
            SDL_DestroyTexture(Thumbnail);
        }

        LoadState = ImageLoadState.NoImage;

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}