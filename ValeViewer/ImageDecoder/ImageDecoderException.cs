namespace ValeViewer.ImageDecoder;

public class ImageDecoderException : Exception
{
    public ImageDecoderException(string? message) : base(message)
    {
    }

    public ImageDecoderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}