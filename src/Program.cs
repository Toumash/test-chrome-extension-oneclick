using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TestChromeExtension;
using TestChromeExtension.GitHub;
using TestChromeExtension.IO;

if (!File.Exists("appsettings.json"))
    throw new Exception("NO appsettings.json file. Please create one");

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();
var cfg = config.Get<Config>()!;

var github = new GitHubApiClient(cfg.GithubToken);
var artifactUrl = await github.GetArtifactUrl(cfg.Repo);

using var extensionPath = new DisposableFile(await github.DownloadArtifact(artifactUrl));
using var extensionDir = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks));
using var tempDir2 = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks + "2"));
Directory.CreateDirectory(extensionDir.Path);
Directory.CreateDirectory(tempDir2.Path);

ZipFile.ExtractToDirectory(extensionPath.Path, extensionDir.Path);

try
{
    Console.WriteLine($"Starting chrome from path {extensionDir.Path}");
    var process = Process.Start(Browser.GetBrowserExecutableLocation(),
        string.Join(" ", Browser.GetOptions(cfg, extensionDir, tempDir2)));
    await process.WaitForExitAsync();
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    throw;
}