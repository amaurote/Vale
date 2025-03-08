using System.Text;
using SDL2;

namespace ValeViewer.Sdl.Utils;

public static class SdlTtfUtils
{
    public static List<string> WrapText(string text, int maxWidth, IntPtr font)
    {
        text = new string(text.Select(c => char.IsControl(c) && c != '\n' ? ' ' : c).ToArray());

        List<string> lines = [];
        var currentLine = new StringBuilder();
        var word = new StringBuilder();

        using var reader = new StringReader(text);
        int nextChar;
        while ((nextChar = reader.Read()) != -1)
        {
            var c = (char)nextChar;

            if (char.IsWhiteSpace(c))
            {
                if (word.Length > 0)
                {
                    ProcessWord(word.ToString(), ref currentLine, lines, maxWidth, font);
                    word.Clear();
                }
            }
            else
            {
                word.Append(c);
            }
        }

        if (word.Length > 0)
            ProcessWord(word.ToString(), ref currentLine, lines, maxWidth, font);

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString());

        return lines;
    }

    private static void ProcessWord(string word, ref StringBuilder currentLine, List<string> lines, int maxWidth, IntPtr font)
    {
        if (SDL_ttf.TTF_SizeText(font, word, out var wordWidth, out _) == 0 && wordWidth > maxWidth)
        {
            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            lines.AddRange(SplitLongWord(word, maxWidth, font));
            return;
        }

        var testLine = currentLine.Length == 0 ? word : $"{currentLine} {word}";

        if (SDL_ttf.TTF_SizeText(font, testLine, out var testWidth, out _) == 0 && testWidth > maxWidth && currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
            currentLine.Clear();
            currentLine.Append(word);
        }
        else
        {
            if (currentLine.Length > 0) currentLine.Append(' ');
            currentLine.Append(word);
        }
    }

    private static List<string> SplitLongWord(string word, int maxWidth, IntPtr font)
    {
        List<string> parts = [];
        var currentPart = new StringBuilder();

        foreach (var c in word)
        {
            var testPart = currentPart.ToString() + c;
            if (SDL_ttf.TTF_SizeText(font, testPart, out var textWidth, out _) == 0 && textWidth > maxWidth && currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
                currentPart.Clear();
            }

            currentPart.Append(c);
        }

        if (currentPart.Length > 0)
            parts.Add(currentPart.ToString());

        return parts;
    }
}