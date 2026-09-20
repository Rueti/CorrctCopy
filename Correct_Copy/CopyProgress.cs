using System.Globalization;
using System.Text.RegularExpressions;

namespace Correct_Copy;

public sealed class CopyProgress
{
    private string tail = "";
    public double? Read(string text)
    {
        var combined = tail + text;
        double? result = null;
        foreach (Match match in Regex.Matches(combined, @"(?:^|[\r\n])[ \t]*(\d{1,3}(?:[.,]\d+)?)\s*%"))
            if (double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value) && value <= 100) result = value;
        tail = combined.Length > 256 ? combined[^256..] : combined;
        return result;
    }
}
