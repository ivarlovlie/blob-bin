using System.Text.RegularExpressions;

namespace BlobBin;

public static class Tools
{
    public static string GetFilesDirectoryPath(bool createIfNotExists = false) {
        var filesDirectoryPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "AppData",
            "files"
        );
        if (createIfNotExists) Directory.CreateDirectory(filesDirectoryPath);
        return filesDirectoryPath;
    }

    public static TimeSpan ParseHumanTimeSpan(string dateTime) {
        var ts = TimeSpan.Zero;
        var currentString = "";
        var currentNumber = "";
        foreach (var ch in dateTime + ' ') {
            currentString += ch;
            if (Regex.IsMatch(currentString, @"^(years(\d|\s)|year(\d|\s)|y(\d|\s))", RegexOptions.IgnoreCase)) {
                ts = ts.Add(TimeSpan.FromDays(365 * int.Parse(currentNumber)));
                currentString = "";
                currentNumber = "";
            }

            if (Regex.IsMatch(currentString, @"^(weeks(\d|\s)|week(\d|\s)|w(\d|\s))", RegexOptions.IgnoreCase)) {
                ts = ts.Add(TimeSpan.FromDays(7 * int.Parse(currentNumber)));
                currentString = "";
                currentNumber = "";
            }

            if (Regex.IsMatch(currentString, @"^(days(\d|\s)|day(\d|\s)|d(\d|\s))", RegexOptions.IgnoreCase)) {
                ts = ts.Add(TimeSpan.FromDays(int.Parse(currentNumber)));
                currentString = "";
                currentNumber = "";
            }

            if (Regex.IsMatch(currentString, @"^(hours(\d|\s)|hour(\d|\s)|h(\d|\s))", RegexOptions.IgnoreCase)) {
                ts = ts.Add(TimeSpan.FromHours(int.Parse(currentNumber)));
                currentString = "";
                currentNumber = "";
            }

            if (Regex.IsMatch(currentString, @"^(mins(\d|\s)|min(\d|\s)|m(\d|\s))", RegexOptions.IgnoreCase)) {
                ts = ts.Add(TimeSpan.FromMinutes(int.Parse(currentNumber)));
                currentString = "";
                currentNumber = "";
            }

            if (Regex.IsMatch(currentString, @"^(secs(\d|\s)|sec(\d|\s)|s(\d|\s))", RegexOptions.IgnoreCase)) {
                ts = ts.Add(TimeSpan.FromSeconds(int.Parse(currentNumber)));
                currentString = "";
                currentNumber = "";
            }

            if (Regex.IsMatch(ch.ToString(), @"\d")) {
                currentNumber += ch;
                currentString = "";
            }
        }

        return ts;
    }
}