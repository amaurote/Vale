namespace ValeViewer.Decoder.Utils;

public static class MetadataProcessor
{
    private static readonly Dictionary<string, string> ExifMetadataMap = new()
    {
        // Camera & Lens Information
        { "Make", "Make" },
        { "Model", "Model" },
        { "Lens", "Lens" },
        // { "Focal Length", "FocalLength" },
        // { "Focal Length (35mm Eq.)", "FocalLengthIn35mmFormat" },

        // Exposure & Settings
        { "Exposure Time", "ExposureTime" },
        { "F-Number", "FNumber" },
        { "ISO Speed Ratings", "ISO" },
        // { "Exposure Compensation", "ExposureBiasValue" },
        // { "Flash", "Flash" },

        // Date & Time
        { "Date/Time", "Taken" },

        // GPS Position (Check only if available)
        { "GPS Latitude", "GPSLatitude" },
        { "GPS Longitude", "GPSLongitude" },
        // { "GPS Altitude", "GPSAltitude" },

        // Image Details
        // { "Orientation", "Orientation" },
        // { "Color Space", "ColorProfile" },
        // { "Data Precision", "BitDepth" },

        // Software / Processing
        // { "Software", "Software" },
    };
    
    public static Dictionary<string, string> ProcessMetadata(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var processed = new Dictionary<string, string>();
        foreach (var directory in directories)
        foreach (var tag in directory.Tags)
            if (ExifMetadataMap.TryGetValue(tag.Name, out var replacedKey))
                processed[replacedKey] = tag.Description ?? "N/A";

        return processed;
    }
}