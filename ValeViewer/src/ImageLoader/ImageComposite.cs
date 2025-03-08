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

    private string? FilePath { get; set; }
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }

    public CompositeState LoadState { get; private set; } = CompositeState.Empty;
    
    public int RenderedWidth { get; set; }
    public int RenderedHeight { get; set; }

    private CancellationTokenSource? _cancellationTokenSource;

    public ImageComposite()
    {
    }

    public ImageComposite(string filePath)
    {
        FilePath = filePath;
    }

    public async Task LoadImageAsync(IntPtr renderer)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            Logger.Log($"[ImageComposite] File path undefined!", Logger.LogLevel.Error);
            return;
        }
        
        _cancellationTokenSource?.Cancel(); // Cancel previous task if still running
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        if (LoadState != CompositeState.Empty)
            Dispose();

        LoadState = CompositeState.Loading;

        try
        {
            FileSize = new FileInfo(FilePath).Length;
            FileName = Path.GetFileName(FilePath);
            var extension = Path.GetExtension(FilePath);
            ExpectedLoadTime = LoadTimeEstimator.EstimateLoadTime(extension, FileSize);

            var stopwatch = Stopwatch.StartNew();

            var decoder = await ImageDecoderFactory.GetImageDecoderAsync(FilePath);
            using var imageData = await decoder.DecodeAsync(FilePath).ConfigureAwait(false);

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
            LoadState = CompositeState.ImageLoaded;
        }
        catch (TaskCanceledException)
        {
        }
        catch (ImageDecodeException ex)
        {
            LoadState = CompositeState.Failed;
            Logger.Log(ex.Message, Logger.LogLevel.Warn);
        }
        catch (FileNotFoundException)
        {
            LoadState = CompositeState.Failed;
            Logger.Log($"[ImageComposite] Failed to load image: {FilePath}", Logger.LogLevel.Error);
        }
        catch (Exception ex)
        {
            LoadState = CompositeState.Failed;
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

        LoadState = CompositeState.Empty;
        RenderedWidth = 0;
        RenderedHeight = 0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}