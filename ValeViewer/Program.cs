using System.Runtime.InteropServices;
using SDL2;
using ValeViewer.Sdl;

namespace ValeViewer;

class Program
{
    static void Main(string[] args)
    {
        // Manually set the SDL2 library path for macOS
        // FIXME
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            NativeLibrary.SetDllImportResolver(typeof(SDL).Assembly, (libraryName, assembly, searchPath) =>
            {
                return libraryName switch
                {
                    "SDL2" or "SDL2.dll" => NativeLibrary.Load("/opt/homebrew/lib/libSDL2.dylib"),
                    "SDL2_ttf" or "SDL2_ttf.dll" => NativeLibrary.Load("/opt/homebrew/lib/libSDL2_ttf.dylib"),
                    _ => IntPtr.Zero
                };
            });
        }

        var imagePath = args.Length > 0 ? args[0] : null;
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            imagePath = null;
        
        try
        {
            using var viewer = new SdlCore(imagePath);
            viewer.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}