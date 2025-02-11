using System.Reflection;
using System.Runtime.InteropServices;
using LibHeifSharp;
using SDL2;

namespace ValeViewer;

public static class NativeLibraryLoader
{
    private static readonly string LibPath;
    private static readonly string SystemName;
    private static readonly Dictionary<string, string> PathDictionary = new();

    static NativeLibraryLoader()
    {
        var basePath = AppContext.BaseDirectory;
        var macOsFrameworkPath = Path.Combine(basePath, "..", "Frameworks"); // Adjusted for .app bundle
        LibPath = Path.Combine(basePath, "lib");

        SystemName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "macOS";

        var platformLibraries = new Dictionary<string, string>
        {
            {
                "SDL2", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SDL2.dll" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "libSDL2.so" :
                "libSDL2.dylib"
            },

            {
                "SDL2_TTF", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SDL2_ttf.dll" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "libSDL2_ttf.so" :
                "libSDL2_ttf.dylib"
            },

            {
                "LIBHEIF", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libheif.dll" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "libheif.so" :
                "libheif.dylib"
            }
        };

        foreach (var (id, libName) in platformLibraries)
        {
            LoadLibrary(macOsFrameworkPath, libName, id);
        }

        ResolveLibraries();
    }

    private static void LoadLibrary(string basePath, string libraryName, string identifier)
    {
        var libFilePath = Path.Combine(basePath, libraryName);

        if (File.Exists(libFilePath))
        {
            PathDictionary[identifier] = libFilePath;
        }
        else
        {
            Logger.Log($"[NativeLibraryLoader] Warning: {libFilePath} not found. Attempting fallback.");
            var fallbackPath = Path.Combine(LibPath, SystemName, libraryName); // Use predefined OS folder names
            Logger.Log($"[NativeLibraryLoader] Fallback path: {fallbackPath}");
            if (File.Exists(fallbackPath))
            {
                PathDictionary[identifier] = fallbackPath;
            }
            else
            {
                Logger.Log($"[NativeLibraryLoader] Failed to locate {libraryName}.");
            }
        }
    }

    private static void ResolveLibraries()
    {
        NativeLibrary.SetDllImportResolver(typeof(SDL).Assembly, ResolveSdl);
        NativeLibrary.SetDllImportResolver(typeof(LibHeifInfo).Assembly, ResolveHeif);
    }

    private static IntPtr ResolveSdl(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        return libraryName switch
        {
            "SDL2" or "SDL2.dll" or "libSDL2.so" => NativeLibrary.Load(PathDictionary["SDL2"]),
            "SDL2_ttf" or "SDL2_ttf.dll" or "libSDL2_ttf.so" => NativeLibrary.Load(PathDictionary["SDL2_TTF"]),
            _ => IntPtr.Zero
        };
    }

    private static IntPtr ResolveHeif(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        return libraryName switch
        {
            "libheif" or "libheif.dll" or "libheif.so" => NativeLibrary.Load(PathDictionary["LIBHEIF"]),
            _ => IntPtr.Zero
        };
    }

    // Force class initialization
    public static readonly object Instance = new();
}