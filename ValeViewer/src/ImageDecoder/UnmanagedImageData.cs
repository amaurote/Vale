namespace ValeViewer.ImageDecoder;

public readonly struct UnmanagedImageData(
    int width,
    int height,
    IntPtr pixelData,
    Action<IntPtr> freeMemoryAction,
    Dictionary<string, string>? metadata = null
) : IDisposable
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public IntPtr PixelData { get; } = pixelData;

    private readonly Action<IntPtr>? _freeMemoryAction = freeMemoryAction;
    public Dictionary<string, string> Metadata { get; } = metadata ?? new Dictionary<string, string>();

    public void Dispose()
    {
        if (PixelData != IntPtr.Zero && _freeMemoryAction != null)
        {
            _freeMemoryAction(PixelData);
        }
    }
}