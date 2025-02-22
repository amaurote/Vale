namespace ValeViewer.Static;

public static class LoadTimeEstimator
{
    private static readonly Dictionary<(string Extension, int SizeBucket), List<double>> LoadTimeData = new();

    public static void RecordTime(string extension, long sizeInBytes, double loadTime)
    {
        var ext = ProcessExtension(extension);
        if(ext == null)
            return;
         
        var sizeBucket = GetSizeBucket(sizeInBytes);
        var key = (ext, sizeBucket);

        if (!LoadTimeData.ContainsKey(key))
            LoadTimeData[key] = [];

        LoadTimeData[key].Add(loadTime);

        // Keep only the last 20 records for each bucket to avoid excessive memory use
        if (LoadTimeData[key].Count > 20)
            LoadTimeData[key].RemoveAt(0);
    }

    public static double EstimateLoadTime(string extension, long sizeInBytes)
    {
        var ext = ProcessExtension(extension);
        if(ext == null)
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

    private static int GetSizeBucket(long sizeInBytes)
    {
        // Bucket sizes: 256KB, 512KB, 1MB, 2MB, 4MB, etc.
        var bucket = (int)Math.Pow(2, Math.Ceiling(Math.Log(sizeInBytes / 256000.0, 2)));
        return Math.Max(bucket, 1); // Ensure minimum bucket of 1
    }

    private static string? ProcessExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return null;

        if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "JPEG";

        if (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase))
            return "HEIF";

        return extension.Replace(".", null).ToUpper();
    }
}