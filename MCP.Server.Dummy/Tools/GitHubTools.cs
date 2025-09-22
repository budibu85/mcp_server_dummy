using MCP.Server.Dummy.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace MCP_Server_Local.Tools;

internal class GitHubTools
{
    private readonly GitHubService _gitHubService;

    public GitHubTools(GitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    [McpServerTool]
    [Description("Get information about a specific GitHub repository, including the number of stars.")]
    public async Task<string> GetRepositoryStars(
        [Description("GitHub username or organization name")] string owner,
        [Description("Repository name")] string repositoryName)
    {
        var repository = await _gitHubService.GetRepositoryAsync(owner, repositoryName);
        
        if (repository == null)
        {
            return $"Repository {owner}/{repositoryName} not found or is not accessible.";
        }

        var result = new StringBuilder();
        result.AppendLine($"?? Repository: {repository.FullName}");
        result.AppendLine($"? Stars: {repository.StargazersCount:N0}");
        result.AppendLine($"?? Forks: {repository.ForksCount:N0}");
        
        if (!string.IsNullOrEmpty(repository.Language))
        {
            result.AppendLine($"?? Language: {repository.Language}");
        }
        
        if (!string.IsNullOrEmpty(repository.Description))
        {
            result.AppendLine($"?? Description: {repository.Description}");
        }
        
        result.AppendLine($"?? URL: {repository.HtmlUrl}");
        result.AppendLine($"?? Last Updated: {repository.UpdatedAt:yyyy-MM-dd}");
        
        return result.ToString();
    }

    [McpServerTool]
    [Description("Get all repositories for a GitHub user with their star counts.")]
    public async Task<string> GetUserRepositories(
        [Description("GitHub username")] string username)
    {
        var repositories = await _gitHubService.GetUserRepositoriesAsync(username);
        
        if (repositories.Count == 0)
        {
            return $"No repositories found for user {username} or user does not exist.";
        }

        var result = new StringBuilder();
        result.AppendLine($"?? Repositories for {username}:");
        result.AppendLine();

        var totalStars = repositories.Sum(r => r.StargazersCount);
        var totalForks = repositories.Sum(r => r.ForksCount);
        
        result.AppendLine($"?? Summary:");
        result.AppendLine($"  • Total Repositories: {repositories.Count:N0}");
        result.AppendLine($"  • Total Stars: {totalStars:N0}");
        result.AppendLine($"  • Total Forks: {totalForks:N0}");
        result.AppendLine();

        // Sort by stars descending
        var sortedRepos = repositories
            .Where(r => !r.Private) // Only show public repositories
            .OrderByDescending(r => r.StargazersCount)
            .Take(20) // Limit to top 20 repositories
            .ToList();

        result.AppendLine($"?? Top repositories by stars:");
        
        foreach (var repo in sortedRepos)
        {
            result.AppendLine($"  • {repo.Name} - ? {repo.StargazersCount:N0} stars, ?? {repo.ForksCount:N0} forks");
            
            if (!string.IsNullOrEmpty(repo.Language))
            {
                result.AppendLine($"    Language: {repo.Language}");
            }
            
            if (!string.IsNullOrEmpty(repo.Description))
            {
                var shortDescription = repo.Description.Length > 100 
                    ? repo.Description.Substring(0, 100) + "..." 
                    : repo.Description;
                result.AppendLine($"    Description: {shortDescription}");
            }
            
            result.AppendLine($"    URL: {repo.HtmlUrl}");
            result.AppendLine();
        }

        return result.ToString();
    }

    [McpServerTool]
    [Description("Get star statistics summary for a GitHub user.")]
    public async Task<string> GetUserStarsSummary(
        [Description("GitHub username")] string username)
    {
        var repositories = await _gitHubService.GetUserRepositoriesAsync(username);
        
        if (repositories.Count == 0)
        {
            return $"No repositories found for user {username} or user does not exist.";
        }

        var publicRepos = repositories.Where(r => !r.Private).ToList();
        var totalStars = publicRepos.Sum(r => r.StargazersCount);
        var totalForks = publicRepos.Sum(r => r.ForksCount);
        var mostStarredRepo = publicRepos.OrderByDescending(r => r.StargazersCount).FirstOrDefault();
        
        var languageStats = publicRepos
            .Where(r => !string.IsNullOrEmpty(r.Language))
            .GroupBy(r => r.Language)
            .Select(g => new { Language = g.Key, Count = g.Count(), Stars = g.Sum(r => r.StargazersCount) })
            .OrderByDescending(l => l.Stars)
            .Take(5)
            .ToList();

        var result = new StringBuilder();
        result.AppendLine($"?? GitHub Stars Summary for {username}");
        result.AppendLine("?".PadRight(50, '?'));
        result.AppendLine();
        
        result.AppendLine($"?? Overall Statistics:");
        result.AppendLine($"  • Total Public Repositories: {publicRepos.Count:N0}");
        result.AppendLine($"  • Total Stars: {totalStars:N0}");
        result.AppendLine($"  • Total Forks: {totalForks:N0}");
        result.AppendLine($"  • Average Stars per Repo: {(publicRepos.Count > 0 ? (double)totalStars / publicRepos.Count : 0):F1}");
        result.AppendLine();

        if (mostStarredRepo != null)
        {
            result.AppendLine($"?? Most Starred Repository:");
            result.AppendLine($"  • {mostStarredRepo.Name} - {mostStarredRepo.StargazersCount:N0} stars");
            result.AppendLine($"  • {mostStarredRepo.HtmlUrl}");
            result.AppendLine();
        }

        if (languageStats.Any())
        {
            result.AppendLine($"?? Top Languages by Stars:");
            foreach (var lang in languageStats)
            {
                result.AppendLine($"  • {lang.Language}: {lang.Stars:N0} stars ({lang.Count} repos)");
            }
        }

        return result.ToString();
    }
}