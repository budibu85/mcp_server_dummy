using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCP.Server.Dummy.Services
{
    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        
        public GitHubService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MCP-Server-Dummy/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        public async Task<GitHubRepository?> GetRepositoryAsync(string owner, string repositoryName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repositoryName}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var repository = JsonSerializer.Deserialize<GitHubRepository>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return repository;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<GitHubRepository>> GetUserRepositoriesAsync(string username)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.github.com/users/{username}/repos?sort=updated&per_page=100");
                
                if (!response.IsSuccessStatusCode)
                {
                    return new List<GitHubRepository>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var repositories = JsonSerializer.Deserialize<List<GitHubRepository>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<GitHubRepository>();

                return repositories;
            }
            catch (Exception)
            {
                return new List<GitHubRepository>();
            }
        }
    }

    public class GitHubRepository
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int StargazersCount { get; set; }
        public int ForksCount { get; set; }
        public string Language { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string HtmlUrl { get; set; } = string.Empty;
        public bool Private { get; set; }
    }
}
