using System.Runtime.InteropServices;

namespace ValeViewer.Sdl;

public static class TtfLoader
{
    // TODO store some default font in resources
    public static string GetDefaultFontPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/System/Library/Fonts/Supplemental/Arial.ttf";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        }

        throw new Exception("Unable to locate default font.");
    }
}