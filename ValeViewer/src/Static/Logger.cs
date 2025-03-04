namespace ValeViewer.Static;

public static class Logger
{
    private static readonly string LogFilePath = Path.Combine(ValeDataDirectory.GetDataDirectory(), "log.txt");
    
    private static readonly Lock Lock = new();

    private static LogStrategy _currentStrategy = LogStrategy.File;
    private const long MaxLogFileSize = 5 * 1024 * 1024; // 5 MB max log size

    public static void Log(string message, LogLevel level = LogLevel.Info, bool includeTimestamp = true)
    {
        if (_currentStrategy == LogStrategy.Disabled) return;

        var logEntry = includeTimestamp
            ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}"
            : $"[{level}] {message}";

        lock (Lock) // Ensure thread safety
        {
            if (_currentStrategy is LogStrategy.Console or LogStrategy.Both)
            {
                Console.ForegroundColor = level switch
                {
                    LogLevel.Info => ConsoleColor.White,
                    LogLevel.Warn => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.Gray
                };

                Console.WriteLine(logEntry);
                Console.ResetColor();
            }

            if (_currentStrategy is LogStrategy.File or LogStrategy.Both)
            {
                try
                {
                    if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogFileSize)
                    {
                        File.WriteAllText(LogFilePath, string.Empty); // Truncate if too large
                    }

                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Logger] Failed to write to log: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }

    public static void ClearLog()
    {
        lock (Lock)
        {
            try
            {
                File.WriteAllText(LogFilePath, string.Empty);
                Console.WriteLine("[Logger] Log file cleared.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger] Failed to clear log: {ex.Message}");
            }
        }
    }

    public static void SetLogStrategy(LogStrategy strategy)
    {
        _currentStrategy = strategy;
    }

    public enum LogStrategy
    {
        Disabled,
        Console,
        File,
        Both
    }

    public enum LogLevel
    {
        Info,
        Warn,
        Error
    }
}