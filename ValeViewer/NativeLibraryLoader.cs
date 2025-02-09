using System.Runtime.InteropServices;
using SDL2;
using LibHeifSharp;

namespace ValeViewer;

public static class NativeLibraryLoader
{
    static NativeLibraryLoader()
    {
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
            
            NativeLibrary.SetDllImportResolver(typeof(LibHeifInfo).Assembly, (libraryName, assembly, searchPath) =>
            {
                return libraryName switch
                {
                    "libheif" => NativeLibrary.Load("/opt/homebrew/lib/libheif.dylib"),
                    _ => IntPtr.Zero
                };
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            NativeLibrary.SetDllImportResolver(typeof(SDL).Assembly, (libraryName, assembly, searchPath) =>
            {
                return libraryName switch
                {
                    "SDL2" or "libSDL2.so" => NativeLibrary.Load("libSDL2.so"),
                    "SDL2_ttf" or "libSDL2_ttf.so" => NativeLibrary.Load("libSDL2_ttf.so"),
                    _ => IntPtr.Zero
                };
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            NativeLibrary.SetDllImportResolver(typeof(SDL).Assembly, (libraryName, assembly, searchPath) =>
            {
                return libraryName switch
                {
                    "SDL2" or "SDL2.dll" => NativeLibrary.Load("SDL2.dll"),
                    "SDL2_ttf" or "SDL2_ttf.dll" => NativeLibrary.Load("SDL2_ttf.dll"),
                    _ => IntPtr.Zero
                };
            });
        }
    }

    // Force the class to be initialized
    public static readonly object Instance = new();
}