using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TestChromeExtension.Exceptions;
using TestChromeExtension.IO;

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
            .Select(s => Path.Combine(s, suffix));
        var chromeLocation = possibleLocations.FirstOrDefault(File.Exists);

        return chromeLocation ?? throw new BrowserNotFoundException();
    }

    public static List<string> GetOptions(Config cfg1, DisposableFile disposableFile, DisposableFile tempDir3)
    {
        var list = new List<string>();
        list.AddRange(cfg1.StartUrls.Split(';'));
        list.AddRange(new []
        {
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-translate",
            "--disable-default-apps",
            "--disable-popup-blocking",
            "--disable-zero-browsers-open-for-tests",
            $"--load-extension=\"{disposableFile.Path}\"",
            "--new-window",
            $"--user-data-dir=\"{tempDir3.Path}\"",
            "--system-developer-mode"
        });
        return list;
    }
}