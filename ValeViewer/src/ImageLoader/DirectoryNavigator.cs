using ValeViewer.Static;

namespace ValeViewer.ImageLoader;

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
    private static string? _anchorFile = string.Empty;

    private static List<string> _imageList = [];
    private static int _currentIndex = -1;

    public static void SearchImages(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.Exists(path))
        {
            throw new ArgumentException("[DirectoryNavigator] Invalid path!", nameof(path));
        }

        if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
        {
            _currentDirectory = path;
            _anchorFile = null;
        }
        else
        {
            _anchorFile = path;
            _currentDirectory = Path.GetDirectoryName(path) ?? throw new ArgumentException("[DirectoryNavigator] Invalid path!", nameof(path));
        }

        SearchImagesInternal();
    }

    private static void SearchImagesInternal()
    {
        Logger.Log($"[DirectoryNavigator] Searching inside: {_currentDirectory}");

        _imageList = Directory.GetFiles(_currentDirectory)
            .Where(file => ImageExtensions.Contains(Path.GetExtension(file).ToLower()) || (_anchorFile != null && file == _anchorFile))
            .Distinct()
            .OrderBy(Path.GetFileName)
            .ToList();

        _currentIndex = (_anchorFile != null)
            ? _imageList.IndexOf(_anchorFile)
            : (_imageList.Count > 0)
                ? 0
                : -1;
    }

    public static string? MoveToNext()
    {
        if (_imageList.Count == 0 || _currentIndex < 0)
            return null;

        if (_currentIndex < _imageList.Count - 1)
            _currentIndex++;

        return _imageList[_currentIndex];
    }

    public static string? GetCurrent()
    {
        if (_imageList.Count > 0 && _currentIndex >= 0 && _currentIndex < _imageList.Count)
            return _imageList[_currentIndex];

        return null;
    }

    public static string? MoveToPrevious()
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

    public static string? MoveToFirst()
    {
        if (_imageList.Count == 0)
            return null;

        _currentIndex = 0;
        return _imageList[_currentIndex];
    }

    public static string? MoveToLast()
    {
        if (_imageList.Count == 0)
            return null;

        _currentIndex = _imageList.Count - 1;
        return _imageList[_currentIndex];
    }

    public static List<string> GetAdjacent(int depth)
    {
        List<string> paths = [];

        if (depth < 1 || _imageList.Count == 0)
            return paths;

        var start = Math.Max(0, _currentIndex - depth);
        var end = Math.Min(_imageList.Count - 1, _currentIndex + depth);

        for (var i = start; i <= end; i++)
        {
            if (i != _currentIndex)
                paths.Add(_imageList[i]);
        }

        return paths;
    }


    public static (int index, int count) GetIndex() => (_currentIndex + 1, _imageList.Count);
}