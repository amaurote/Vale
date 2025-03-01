using SDL2;

namespace ValeViewer.Sdl.Utils;

public static class SdlTtfUtils
{
    public static List<string> WrapText(string text, int maxWidth, IntPtr font)
    {
        text = new string(text.Select(c => char.IsControl(c) && c != '\n' ? ' ' : c).ToArray());

        List<string> lines = [];
        var currentLine = "";

        foreach (var word in text.Split(' '))
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";

            if (SDL_ttf.TTF_SizeText(font, word, out var wordWidth, out _) == 0 && wordWidth > maxWidth)
            {
                // Ensure the current line is added before handling the long word
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                // Add split parts of the long word
                lines.AddRange(SplitLongWord(word, maxWidth, font));
                continue;
            }

            if (SDL_ttf.TTF_SizeText(font, testLine, out var testWidth, out _) == 0 && testWidth > maxWidth && currentLine.Length > 0)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    public static List<string> SplitLongWord(string word, int maxWidth, IntPtr font)
    {
        List<string> parts = [];
        var currentPart = "";

        foreach (var c in word)
        {
            var testPart = currentPart + c;
            if (SDL_ttf.TTF_SizeText(font, testPart, out var textWidth, out _) == 0 && textWidth > maxWidth && currentPart.Length > 0)
            {
                parts.Add(currentPart);
                currentPart = $"{c}";
            }
            else
            {
                currentPart = testPart;
            }
        }

        if (!string.IsNullOrEmpty(currentPart))
            parts.Add(currentPart);

        return parts;
    }
}