using System.Text;

namespace Correct_Copy;

// Robocopy mixes OEM diagnostic/header text and UTF-16LE output in one pipe.
// A line can even start in OEM and continue in Unicode (e.g. "Optionen:").
public sealed class RobocopyOutput(Encoding oemEncoding)
{
    private readonly List<byte> pending = [];
    private bool unicode;

    public string Decode(ReadOnlySpan<byte> bytes, bool complete = false)
    {
        foreach (var b in bytes) pending.Add(b);
        var output = new StringBuilder();
        var ansi = new List<byte>();
        void FlushAnsi()
        {
            if (ansi.Count == 0) return;
            output.Append(oemEncoding.GetString(ansi.ToArray())); ansi.Clear();
        }
        int i = 0;
        while (i + 1 < pending.Count)
        {
            byte first = pending[i], second = pending[i + 1];
            if (!unicode && first == 0xFF && second == 0xFE) { FlushAnsi(); unicode = true; i += 2; continue; }
            if (!unicode && second == 0) { FlushAnsi(); unicode = true; }
            if (unicode)
            {
                // Some Robocopy writes finish a Unicode segment with an OEM newline.
                if ((first is 10 or 13) && second != 0) { unicode = false; continue; }
                char c = (char)(first | second << 8);
                output.Append(c); i += 2;
                if (c == '\n') unicode = false;
            }
            else { ansi.Add(first); i++; }
        }
        if (complete && i < pending.Count)
        {
            if (pending[i] != 0) ansi.Add(pending[i]);
            i++;
        }
        FlushAnsi();
        pending.RemoveRange(0, i);
        return output.ToString();
    }
}
