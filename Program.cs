using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace TestChromeExtension
{
    class DisposableFile : IDisposable
    {
        public DisposableFile(string path)
        {
            Path = path;
        }


        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);

            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
    class Config
    {
        public string Repo { get; set; }
        public string GithubToken { get; set; }
        public string StartUrl { get; set; }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            if (!File.Exists("appsettings.json"))
                throw new Exception("NO appsettings.json file. Please create one");

            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
               .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
               .AddEnvironmentVariables()
               .AddCommandLine(args)
               .Build();
            var token = config.GetValue<string>("GithubToken");

            var startUrl = config.GetValue<string>("StartUrl");
            var repoUrl = config.GetValue<string>("Repo");

            var artifactUrl = await GetArtifactUrl(token, repoUrl);
            using var extensionPath = new DisposableFile(await DownloadArtifact(artifactUrl, token));
            using var tempDir = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks));
            using var tempDir2 = new DisposableFile(Path.Combine(Path.GetTempPath() + "ext" + DateTime.Now.Ticks + "2"));
            Directory.CreateDirectory(tempDir.Path);
            Directory.CreateDirectory(tempDir2.Path);

            ZipFile.ExtractToDirectory(extensionPath.Path, tempDir.Path);
            var extensionDir = tempDir;

            var suffix = "Google\\Chrome\\Application\\chrome.exe";
            var prefixes = new List<string>() {
                Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                Environment.GetEnvironmentVariable("PROGRAMFILES"),
                Environment.GetEnvironmentVariable("PROGRAMFILES(X86)")
            };
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

            var options = new List<string>()
            {
                startUrl,
                "--no-first-run",
                "--no-default-browser-check",
                "--disable-translate",
                "--disable-default-apps",
                "--disable-popup-blocking",
                "--disable-zero-browsers-open-for-tests",
                $"--load-extension=\"{extensionDir.Path}\"",
                "--new-window",
                $"--user-data-dir=\"{tempDir2.Path}\""
            };
            try
            {
                Console.WriteLine($"Starting chrome from path {extensionDir.Path}");
                var process = Process.Start(chromeLocation,
                  string.Join(" ", options));
                await process.WaitForExitAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="personalAccessToken"></param>
        /// <param name="repoUrl">example: toumash/daily-status</param>
        /// <returns></returns>
        public static async Task<string> GetArtifactUrl(string personalAccessToken, string repoUrl)
        {
            var client = new RestClient($"https://api.github.com/repos/{repoUrl}/actions/artifacts?per_page=10&page=1");
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {personalAccessToken}");
            var response = await client.ExecuteAsync<Root>(request);
            return response.Data.artifacts.First().archive_download_url;
        }
        public static async Task<string> DownloadArtifact(string url, string personalAccessToken)
        {
            var client = new RestClient(url);
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {personalAccessToken}");
            var file = client.DownloadData(request);
            var filePath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(filePath, file);
            return filePath;
        }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Artifact
    {
        public int id { get; set; }
        public string node_id { get; set; }
        public string name { get; set; }
        public int size_in_bytes { get; set; }
        public string url { get; set; }
        public string archive_download_url { get; set; }
        public bool expired { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime expires_at { get; set; }
    }

    public class Root
    {
        public int total_count { get; set; }
        public List<Artifact> artifacts { get; set; }
    }
}
