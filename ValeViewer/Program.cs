using SdlCore = ValeViewer.Sdl.Core.SdlCore;

namespace ValeViewer;

class Program
{
    static void Main(string[] args)
    {
        // Ensure NativeLibraryLoader is initialized
        _ = NativeLibraryLoader.Instance;
        
        var imagePath = args.Length > 0 ? args[0] : null;
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            imagePath = null;
        
        try
        {
            using var viewer = new SdlCore(imagePath, true);
            viewer.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}