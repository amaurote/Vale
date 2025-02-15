namespace ValeViewer.Files;

public static class DirectoryNavigator
{
    private static readonly string[] ImageExtensions =
    [
        ".bmp",
        ".heic", ".heif", ".avif",
        ".jpeg", ".jpg",
        ".png",
        ".tga",
        ".tiff",
        ".webp"
    ];

    private static string _currentDirectory = string.Empty;
    private static string _anchorFile = string.Empty;

    private static List<string> _imageList = [];
    private static int _currentIndex = -1;

    public static void SearchImages(string initialFilePath)
    {
        if (!File.Exists(initialFilePath))
            throw new FileNotFoundException($"[DirectoryNavigator] The file {initialFilePath} was not found.");

        if (_anchorFile.Equals(initialFilePath))
            return;

        var dir = Path.GetDirectoryName(initialFilePath);
        if (dir == null || _currentDirectory.Equals(dir))
            return;

        _anchorFile = initialFilePath;
        _currentDirectory = dir;
        
        SearchImagesInternal();
    }

    private static void SearchImagesInternal()
    {
        Logger.Log($"[DirectoryNavigator] Searching inside: {_currentDirectory}");
        
        _imageList = Directory.GetFiles(_currentDirectory)
            .Where(file => ImageExtensions.Contains(Path.GetExtension(file).ToLower()) || file == _anchorFile)
            .Distinct()
            .OrderBy(Path.GetFileName)
            .ToList();

        _currentIndex = _imageList.IndexOf(_anchorFile);
    }

    public static string? Next()
    {
        if (_imageList.Count == 0 || _currentIndex < 0)
            return null;

        if (_currentIndex < _imageList.Count - 1)
            _currentIndex++;

        return _imageList[_currentIndex];
    }

    public static string? Current()
    {
        if (_imageList.Count > 0 && _currentIndex >= 0 && _currentIndex < _imageList.Count)
            return _imageList[_currentIndex];

        return null;
    }

    public static string? Previous()
    {
        if (_imageList.Count == 0)
            return null;

        if (_currentIndex > 0)
            _currentIndex--;

        return _imageList[_currentIndex];
    }

    public static bool HasNext()
    {
        return _imageList.Count > 0 && _currentIndex < _imageList.Count - 1;
    }

    public static bool HasPrevious()
    {
        return _imageList.Count > 0 && _currentIndex > 0;
    }

    public static string? First()
    {
        if (_imageList.Count == 0)
            return null;
        
        _currentIndex = 0;
        return _imageList[_currentIndex];
    }

    public static string? Last()
    {
        if (_imageList.Count == 0)
            return null;
        
        _currentIndex = _imageList.Count - 1;
        return _imageList[_currentIndex];
    }
    
    public static (int index, int count) GetIndex() => (_currentIndex + 1, _imageList.Count);
}