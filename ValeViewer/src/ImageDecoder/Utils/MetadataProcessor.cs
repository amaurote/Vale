namespace ValeViewer.ImageDecoder.Utils;

public static class MetadataProcessor
{
    private static readonly HashSet<string> ExcludedKeys =
    [
        // TIFF & Image Data
        "Strip Byte Counts", "Rows Per Strip", "Tile Offsets", "Tile Byte Counts",
        "Bits Per Sample", "Samples Per Pixel", "Planar Configuration", "Compression",
        "JPEG Interchange Format", "JPEG Interchange Format Length",
        "EXIF IFD Pointer", "GPS IFD Pointer",
        "Image Width", "Image Height", "Pixel X Dimension", "Pixel Y Dimension",
        "YCbCr Coefficients", "YCbCr Sub-Sampling", "YCbCr Positioning",
        "Reference Black White",

        // Color Profile Data
        "Red Colorant", "Green Colorant", "Blue Colorant",
        "Red TRC", "Green TRC", "Blue TRC",
        "White Point", "Primary Chromaticities",
        "Profile Description",

        // Camera-Specific Data
        "Focal Plane X Resolution", "Focal Plane Y Resolution",
        "Focal Plane Resolution Unit", "Lens Serial Number",
        "Scene Capture Type",

        // Versioning & Interoperability
        "Interoperability Index", "Interoperability Version",
        "Exif Version", "FlashPix Version",
        "Thumbnail Offset", "Thumbnail Length",

        // GPS
        "GPS Latitude", "GPS Longitude",
        "GPS Latitude Ref", "GPS Longitude Ref",
        "GPS Altitude", "GPS Altitude Ref",
        "GPS Speed", "GPS Speed Ref",
        "GPS Time Stamp", "GPS Time-Stamp", "GPS Date Stamp",
        "GPS Satellites", "GPS Status",
        "GPS Measure Mode", "GPS Processing Method",
        "GPS Area Information", "GPS Differential",
        "GPS Track", "GPS Track Ref",
        "GPS Img Direction", "GPS Img Direction Ref",
        "GPS Dest Bearing", "GPS Dest Bearing Ref",
        "GPS Horizontal Positioning Error",
    ];

    private static readonly Dictionary<string, string> ReplacedKeys = new()
    {
        { "Make", "Camera Manufacturer" },
        { "Model", "Camera Model" },
        { "Date/Time Original", "Date Taken" },
        { "Create Date", "File Created" }
    };

    public static SortedDictionary<string, string> ProcessMetadata(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        if (directories.Count == 0)
            return new SortedDictionary<string, string> { { "[No EXIF data]", "" } };

        var excludedCount = 0;
        var processed = new SortedDictionary<string, string>();
        foreach (var directory in directories)
        {
            foreach (var tag in directory.Tags)
            {
                if (!ExcludedKeys.Contains(tag.Name))
                {
                    if (ReplacedKeys.TryGetValue(tag.Name, out var replacedKey))
                    {
                        processed[replacedKey] = tag.Description ?? "N/A";
                    }
                    else
                    {
                        processed[tag.Name] = tag.Description ?? "N/A";
                    }
                }
                else
                {
                    excludedCount++;
                }
            }
        }

        if (excludedCount > 0)
            processed.Add("[Excluded Metadata Count]", excludedCount.ToString());

        return processed;
    }
}