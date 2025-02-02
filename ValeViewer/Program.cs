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
                if (libraryName == "SDL2" || libraryName == "SDL2.dll")
                {
                    return NativeLibrary.Load("/opt/homebrew/lib/libSDL2.dylib");
                }

                if (libraryName == "SDL2_ttf" || libraryName == "SDL2_ttf.dll")
                {
                    return NativeLibrary.Load("/opt/homebrew/lib/libSDL2_ttf.dylib");
                }

                return IntPtr.Zero;
            });
        }

        try
        {
            using var viewer = new SdlCore();
            viewer.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}