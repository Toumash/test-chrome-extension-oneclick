using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TestChromeExtension.GitHub;
using TestChromeExtension.IO;

namespace TestChromeExtension;

class Program
{
    static async Task Main(string[] args)
    {
        if (!File.Exists("appsettings.json"))
            throw new Exception("NO appsettings.json file. Please create one");

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        var cfg = config.Get<Config>();

        var githubClient = new GitHubApiClient(cfg.GithubToken);
        var artifactUrl = await githubClient.GetArtifactUrl(cfg.Repo);
        using var extensionPath = new DisposableFile(await githubClient.DownloadArtifact(artifactUrl));
        using var extensionDir = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks));
        using var tempDir2 =
            new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks + "2"));
        Directory.CreateDirectory(extensionDir.Path);
        Directory.CreateDirectory(tempDir2.Path);

        ZipFile.ExtractToDirectory(extensionPath.Path, extensionDir.Path);

        var browserExecutableLocation = Browser.GetBrowserExecutableLocation();

        var options = new List<string>
        {
            cfg.StartUrl,
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-translate",
            "--disable-default-apps",
            "--disable-popup-blocking",
            "--disable-zero-browsers-open-for-tests",
            $"--load-extension=\"{extensionDir.Path}\"",
            "--new-window",
            // $"--user-data-dir=\"{tempDir2.Path}\""
        };
        
        try
        {
            Console.WriteLine($"Starting chrome from path {extensionDir.Path}");
            var process = Process.Start(browserExecutableLocation,
                string.Join(" ", options));
            await process.WaitForExitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}