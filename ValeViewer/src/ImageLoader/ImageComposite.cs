using System.Diagnostics;
using ValeViewer.ImageDecoder;
using ValeViewer.Sdl.Enum;
using ValeViewer.Static;
using static SDL2.SDL;

namespace ValeViewer.ImageLoader;

public class ImageComposite : IDisposable
{
    public IntPtr Image { get; private set; } = IntPtr.Zero;
    public IntPtr Thumbnail { get; private set; } = IntPtr.Zero;

    public double ExpectedLoadTime { get; private set; }
    public double ActualLoadTime { get; private set; }

    public int Zoom { get; set; } = 100;
    public ImageScaleMode? ScaleMode { get; set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; } = [];

    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }

    public ImageLoadState LoadState { get; private set; } = ImageLoadState.NoImage;
    
    public int RenderedWidth { get; set; }
    public int RenderedHeight { get; set; }

    private CancellationTokenSource? _cancellationTokenSource;

    public async Task LoadImageAsync(string imagePath, IntPtr renderer)
    {
        _cancellationTokenSource?.Cancel(); // Cancel previous task if still running
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        Dispose();

        LoadState = ImageLoadState.Loading;

        try
        {
            FileSize = new FileInfo(imagePath).Length;
            FileName = Path.GetFileName(imagePath);
            var extension = Path.GetExtension(imagePath);
            ExpectedLoadTime = LoadTimeEstimator.EstimateLoadTime(extension, FileSize);
            
            var stopwatch = Stopwatch.StartNew();
            
            var decoder = await ImageDecoderFactory.GetImageDecoderAsync(imagePath);
            using var imageData = await decoder.DecodeAsync(imagePath).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                return;
            }

            Width = imageData.Width;
            Height = imageData.Height;
            Metadata = imageData.Metadata;

            Image = SDL_CreateTexture(
                renderer,
                SDL_PIXELFORMAT_ABGR8888,
                (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                Width, Height
            );

            if (Image == IntPtr.Zero)
                throw new Exception($"[ImageComposite] Failed to create SDL texture: {SDL_GetError()}");

            SDL_UpdateTexture(Image, IntPtr.Zero, imageData.PixelData, Width * 4);
            
            stopwatch.Stop();
            ActualLoadTime = stopwatch.Elapsed.TotalMilliseconds;
            LoadTimeEstimator.RecordLoadTime(extension, FileSize, ActualLoadTime);
            LoadState = ImageLoadState.ImageLoaded;
        }
        catch (TaskCanceledException)
        {
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
    }

    private async Task LoadThumbnailAsync(string imagePath, IntPtr renderer, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        ExpectedLoadTime = 0;
        ActualLoadTime = 0;
        Zoom = 100;
        ScaleMode = null;
        Width = 0;
        Height = 0;
        FileName = string.Empty;
        FileSize = 0;

        if (Image != IntPtr.Zero)
        {
            SDL_DestroyTexture(Image);
            Image = IntPtr.Zero;
        }

        if (Thumbnail != IntPtr.Zero)
        {
            SDL_DestroyTexture(Thumbnail);
            Thumbnail = IntPtr.Zero;
        }

        LoadState = ImageLoadState.NoImage;
        RenderedWidth = 0;
        RenderedHeight = 0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}