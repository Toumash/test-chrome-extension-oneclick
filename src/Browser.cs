using System;
using System.IO;
using System.Linq;
using TestChromeExtension.Exceptions;

namespace TestChromeExtension;

class Browser
{
    public static string GetBrowserExecutableLocation()
    {
        const string suffix = "Google\\Chrome\\Application\\chrome.exe";
        var possibleLocations = new[]
        {
            Environment.GetEnvironmentVariable("LOCALAPPDATA"),
            Environment.GetEnvironmentVariable("PROGRAMFILES"),
            Environment.GetEnvironmentVariable("PROGRAMFILES(X86)")
        }.Where(s => !string.IsNullOrEmpty(s))
            .Select(s=>Path.Combine(s,suffix));
        var chromeLocation= possibleLocations.FirstOrDefault(File.Exists);

        return chromeLocation ?? throw new BrowserNotFoundException();
    }
}