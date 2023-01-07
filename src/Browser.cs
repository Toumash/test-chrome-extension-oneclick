using System;
using System.IO;
using System.Linq;

namespace TestChromeExtension;

class Browser
{
    public static string GetBrowserExecutableLocation()
    {
        var suffix = "Google\\Chrome\\Application\\chrome.exe";
        var prefixes = new[]
        {
            Environment.GetEnvironmentVariable("LOCALAPPDATA"),
            Environment.GetEnvironmentVariable("PROGRAMFILES"),
            Environment.GetEnvironmentVariable("PROGRAMFILES(X86)")
        }.Where(s => !string.IsNullOrEmpty(s));
        string chromeLocation = "";
        foreach (var prefix in prefixes)
        {
            var location = Path.Combine(prefix, suffix);
            if (File.Exists(location))
            {
                chromeLocation = location;
                break;
            }
        }

        return chromeLocation;
    }
}