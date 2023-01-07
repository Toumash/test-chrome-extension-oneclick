using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;

namespace TestChromeExtension.GitHub;

public class GitHubApiClient
{
    private readonly string _personalAccessToken;

    public GitHubApiClient(string personalAccessToken)
    {
        _personalAccessToken = personalAccessToken;
    }

    public async Task<string> GetArtifactUrl(string repoUrl)
    {
        var client = new RestClient($"https://api.github.com/repos/{repoUrl}/actions/artifacts?per_page=10&page=1");
        var request = new RestRequest();
        request.AddHeader("Authorization", $"Bearer {_personalAccessToken}");
        var response = await client.ExecuteAsync<GetArtifactResponse>(request);
        return response.Data.artifacts.First().archive_download_url;
    }

    public async Task<string> DownloadArtifact(string url)
    {
        var client = new RestClient(url);
        var request = new RestRequest();
        request.AddHeader("Authorization", $"Bearer {_personalAccessToken}");
        var file = client.DownloadData(request);
        var filePath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(filePath, file);
        return filePath;
    }
}