using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ValeViewer.Static;

public static class LoadTimeEstimator
{
    private static readonly string TimeFilePath = Path.Combine(ValeDataDirectory.GetDataDirectory(), "load_time_data.yaml");

    private static readonly Dictionary<(string Extension, int SizeBucket), List<double>> LoadTimeData = new();

    private static int _unsavedChangesCount;
    private const int UnsavedChangesThreshold = 5;

    static LoadTimeEstimator()
    {
        LoadTimeDataFromFile();
    }

    public static void RecordLoadTime(string extension, long sizeInBytes, double loadTime)
    {
        var ext = ExtensionToFormat(extension);
        if (ext == null)
            return;

        var sizeBucket = GetSizeBucket(sizeInBytes);
        var key = (ext, sizeBucket);

        if (!LoadTimeData.ContainsKey(key))
            LoadTimeData[key] = [];

        LoadTimeData[key].Add(loadTime);

        // Keep only the last 20 records for each bucket to avoid excessive memory use
        if (LoadTimeData[key].Count > 20)
            LoadTimeData[key].RemoveAt(0);

        // Save periodically
        if (++_unsavedChangesCount >= UnsavedChangesThreshold)
        {
            SaveTimeDataToFile(true);
            _unsavedChangesCount = 0;
        }
    }

    public static double EstimateLoadTime(string extension, long sizeInBytes)
    {
        var ext = ExtensionToFormat(extension);
        if (ext == null)
            return 0;

        var sizeBucket = GetSizeBucket(sizeInBytes);
        var key = (ext, sizeBucket);

        if (LoadTimeData.TryGetValue(key, out var loadTimes))
        {
            // Direct match found, return the average
            return loadTimes.Average();
        }

        // No exact match: Find closest available bucket
        var availableBuckets = LoadTimeData.Keys
            .Where(k => k.Extension.Equals(ext))
            .Select(k => k.SizeBucket)
            .OrderBy(b => Math.Abs(b - sizeBucket))
            .ToList();

        if (availableBuckets.Count == 0)
            return 0; // Default estimate if no data available

        // Interpolate between the closest known buckets
        var closestBucket = availableBuckets.First();
        return LoadTimeData[(ext, closestBucket)].Average();
    }

    public static void SaveTimeDataToFile(bool suppressLogging = false)
    {
        try
        {
            var formattedData = LoadTimeData.ToDictionary(
                entry => $"{entry.Key.Extension}_{entry.Key.SizeBucket}",
                entry => entry.Value
            );

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(formattedData);
            File.WriteAllText(TimeFilePath, yaml);

            if (!suppressLogging)
                Logger.Log("[LoadTimeEstimator] Successfully saved time data.");
        }
        catch (Exception ex)
        {
            Logger.Log($"[LoadTimeEstimator] Failed to save time data: {ex.Message}", Logger.LogLevel.Error);
        }
    }

    private static void LoadTimeDataFromFile()
    {
        if (!File.Exists(TimeFilePath))
        {
            Logger.Log("[LoadTimeEstimator] No existing time data found.");
            return;
        }

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlText = File.ReadAllText(TimeFilePath);
            var data = deserializer.Deserialize<Dictionary<string, List<double>>>(yamlText);

            LoadTimeData.Clear();
            foreach (var entry in data)
            {
                var keyParts = entry.Key.Split('_', 2);
                if (keyParts.Length == 2 && int.TryParse(keyParts[1], out var bucket))
                {
                    LoadTimeData[(keyParts[0], bucket)] = entry.Value;
                }
                else
                {
                    Logger.Log($"[LoadTimeEstimator] Skipping invalid YAML entry: {entry.Key}", Logger.LogLevel.Warn);
                }
            }

            Logger.Log("[LoadTimeEstimator] Successfully loaded time data.");
        }
        catch (Exception ex)
        {
            Logger.Log($"[LoadTimeEstimator] Failed to load time data: {ex.Message}", Logger.LogLevel.Error);
        }
    }

    private static int GetSizeBucket(long sizeInBytes)
    {
        // Bucket sizes: 256KB, 512KB, 1MB, 2MB, 4MB, etc.
        var bucket = (int)Math.Pow(2, Math.Ceiling(Math.Log(sizeInBytes / 256000.0, 2)));
        return Math.Max(bucket, 1); // Ensure minimum bucket of 1
    }

    private static string? ExtensionToFormat(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return null;

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
            return "JPEG";

        if (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase))
            return "HEIF";

        return extension.Replace(".", null).ToUpper();
    }
}