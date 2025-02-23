namespace ValeViewer.Static;

public static class ValeDataDirectory
{
    public static string GetDataDirectory()
    {
        var dataDirectory = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValeViewer")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "ValeViewer");

        Directory.CreateDirectory(dataDirectory);
        return dataDirectory;
    }
}