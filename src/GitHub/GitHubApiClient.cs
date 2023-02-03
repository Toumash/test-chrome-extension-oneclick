using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;

namespace TestChromeExtension.GitHub;

public class GitHubApiClient
{
    private readonly RestClient _client;

    public GitHubApiClient(string personalAccessToken)
    {
        _client = new RestClient("https://api.github.com");
        _client.UseAuthenticator(new JwtAuthenticator(personalAccessToken));
    }

    public async Task<string> GetArtifactUrl(string repoUrl)
    {
        var request = new RestRequest($"/repos/{repoUrl}/actions/artifacts?per_page=10&page=1");
        var response = await _client.ExecuteAsync<GetArtifactResponse>(request);
        response.ThrowIfError();
        
        if (response.Data == null)
        {
            throw new Exception($"Invalid token exception. Response is null. Response Code = {response.StatusCode}");
        }

        return response.Data.artifacts.First().archive_download_url;
    }

    public async Task<string> DownloadArtifact(string url)
    {
        var request = new RestRequest(url);
        var file = _client.DownloadData(request);
        var filePath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(filePath, file);
        return filePath;
    }
}