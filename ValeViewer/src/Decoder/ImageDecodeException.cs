namespace ValeViewer.Decoder;

public class ImageDecodeException : Exception
{
    public ImageDecodeException(string? message) : base(message)
    {
    }

    public ImageDecodeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}