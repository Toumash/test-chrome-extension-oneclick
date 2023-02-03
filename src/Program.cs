using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Spectre.Console;
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
var artifactsTask = github.GetAvailableArtifacts(cfg.Repo);
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Fetching artifact list ", async ctx => { await artifactsTask; });

var artifacts = await artifactsTask;

var selectedArtifact = AnsiConsole.Prompt(
    new SelectionPrompt<Artifact>()
        .Title("Select artifact you want to load to the browser as an [green]chrome extension[/]")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to select)[/]")
        .AddChoices(artifacts)
        .UseConverter(a => a.ToString())
);

AnsiConsole.WriteLine($"Selected {selectedArtifact}. Downloading the package...");

using var extensionPath
    = new DisposableFile(await github.DownloadArtifact(selectedArtifact.Url));
using var extensionDir = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks));
using var userDataDir = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks + "2"));
Directory.CreateDirectory(extensionDir.Path);
Directory.CreateDirectory(userDataDir.Path);

ZipFile.ExtractToDirectory(extensionPath.Path, extensionDir.Path);

try
{
    Console.WriteLine($"Starting chrome from path {extensionDir.Path}");
    var process = Process.Start(Browser.GetBrowserExecutableLocation(),
        string.Join(" ", Browser.GetOptions(cfg, extensionDir, userDataDir)));
    await process.WaitForExitAsync();
    await Task.Delay(TimeSpan.FromSeconds(5)); // to really-really exit the browser
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    throw;
}