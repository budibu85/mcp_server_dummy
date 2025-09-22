using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "MCP-Server-Dummy",
    Command = "dotnet",
    Arguments = [
        "run",
        "--project",
        "../../../../MCP.Server.Dummy/MCP.Server.Dummy.csproj"
      ],
});

var client = await McpClientFactory.CreateAsync(clientTransport);

// Get available tools from the server
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Connected to server with tools: {tool.Name}");
}

using var anthropicClient = new AnthropicClient(new APIAuthentication(builder.Configuration["ANTHROPIC_API_KEY"]))
    .Messages
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var options = new ChatOptions
{
    MaxOutputTokens = 1000,
    ModelId = "claude-sonnet-4-20250514",
    Tools = [.. tools]
};

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("MCP Client Started!");
Console.ResetColor();

PromptForInput();
while (Console.ReadLine() is string query && !"exit".Equals(query, StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(query))
    {
        PromptForInput();
        continue;
    }

    await foreach (var message in anthropicClient.GetStreamingResponseAsync(query, options))
    {
        Console.Write(message);
    }
    Console.WriteLine();

    PromptForInput();
}

//// Print the list of tools available from the server.
//Console.WriteLine("Available tools:");
//foreach (var tool in availableTools)
//{
//    Console.WriteLine($"- {tool.Name}: {tool.Description}");
//}

//Console.WriteLine("\nEnter your request:");
//var request = Console.ReadLine();

//if (string.IsNullOrWhiteSpace(request))
//{
//    Console.WriteLine("No request provided.");
//    Console.ReadKey();
//    return;
//}

//// Determine ALL relevant tools for the request (not just the first one)
//var relevantTools = DetermineAllRelevantToolsFromRequest(request, availableTools);

//if (!relevantTools.Any())
//{
//    Console.WriteLine($"Could not determine appropriate tools for request: '{request}'");
//    Console.WriteLine("Available tools are:");
//    foreach (var tool in availableTools)
//    {
//        Console.WriteLine($"- {tool.Name}");
//    }
//    Console.ReadKey();
//    return;
//}

//Console.WriteLine($"Found {relevantTools.Count} relevant tools for your request:");
//foreach (var tool in relevantTools)
//{
//    Console.WriteLine($"- {tool.Name}");
//}

//// Execute ALL relevant tools
//var results = new List<(string ToolName, CallToolResult Result)>();

//foreach (var selectedTool in relevantTools)
//{
//    Console.WriteLine($"\nExecuting tool: {selectedTool.Name}");

//    try
//    {
//        var result = await client.CallToolAsync(
//            selectedTool.Name,
//            CreateParametersForTool(selectedTool.Name, request),
//            cancellationToken: CancellationToken.None);

//        results.Add((selectedTool.Name, result));

//        Console.WriteLine($"Results from {selectedTool.Name}:");
//        foreach (var content in result.Content)
//        {
//            if (content is TextContentBlock textContent)
//            {
//                Console.WriteLine(textContent.Text);
//            }
//        }
//        Console.WriteLine(new string('-', 50));
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Error executing tool {selectedTool.Name}: {ex.Message}");
//        Console.WriteLine(new string('-', 50));
//    }
//}

//Console.WriteLine($"\nCompleted execution of {results.Count} tools.");
//Console.ReadKey();

//// Helper method to determine ALL relevant tools based on the request
//static List<McpClientTool> DetermineAllRelevantToolsFromRequest(string request, IList<McpClientTool> availableTools)
//{
//    var lowerRequest = request.ToLowerInvariant();
//    var relevantTools = new List<McpClientTool>();

//    // Define keyword mappings for tools with scoring
//    var toolKeywords = new Dictionary<string, (string[] keywords, int priority)>
//    {
//        ["get_repository_stars"] = (["stars", "repository", "repo", "github", "star count"], 1),
//        ["get_user_stars_summary"] = (["summary", "user", "stars", "github", "overview"], 2),
//        ["get_user_repositories"] = (["repositories", "repos", "user", "github", "all repos"], 3)
//    };

//    // Score each tool based on relevance
//    var toolScores = new Dictionary<McpClientTool, int>();

//    foreach (var tool in availableTools)
//    {
//        var score = 0;

//        // Direct name match (highest priority)
//        if (lowerRequest.Contains(tool.Name.ToLowerInvariant()))
//        {
//            score += 100;
//        }

//        // Keyword match
//        if (toolKeywords.TryGetValue(tool.Name, out var toolInfo))
//        {
//            var matchedKeywords = toolInfo.keywords.Count(keyword => lowerRequest.Contains(keyword));
//            score += matchedKeywords * 10;
            
//            // Add priority bonus (lower priority number = higher bonus)
//            score += (10 - toolInfo.priority);
//        }

//        // Description match
//        if (!string.IsNullOrEmpty(tool.Description))
//        {
//            var lowerDescription = tool.Description.ToLowerInvariant();
//            var requestWords = lowerRequest.Split(' ', StringSplitOptions.RemoveEmptyEntries);

//            var descriptionMatches = requestWords.Count(word => 
//                lowerDescription.Contains(word) && word.Length > 3);
//            score += descriptionMatches * 5;
//        }

//        if (score > 0)
//        {
//            toolScores[tool] = score;
//        }
//    }

//    // Return tools sorted by relevance score (highest first)
//    relevantTools = toolScores
//        .OrderByDescending(kvp => kvp.Value)
//        .Select(kvp => kvp.Key)
//        .ToList();

//    // If no tools matched, return all tools as fallback
//    if (!relevantTools.Any())
//    {
//        Console.WriteLine("No specific matches found, will try all available tools.");
//        relevantTools = availableTools.ToList();
//    }

//    return relevantTools;
//}

//// Helper method to create parameters for different tools
//static Dictionary<string, object?> CreateParametersForTool(string toolName, string request)
//{
//    return toolName.ToLowerInvariant() switch
//    {
//        "get_repository_stars" => ExtractRepositoryParameters(request),
//        "get_user_stars_summary" => ExtractUserParameters(request),
//        "get_user_repositories" => ExtractUserParameters(request),
//        _ => new Dictionary<string, object?> { ["message"] = request } // Default fallback
//    };
//}

// Helper method to extract repository parameters
//static Dictionary<string, object?> ExtractRepositoryParameters(string request)
//{
//    // Try to find GitHub repo format (owner/repo)
//    var githubPattern = @"([a-zA-Z0-9\-_\.]+)/([a-zA-Z0-9\-_\.]+)";
//    var match = System.Text.RegularExpressions.Regex.Match(request, githubPattern);
    
//    if (match.Success)
//    {
//        return new Dictionary<string, object?>
//        {
//            ["owner"] = match.Groups[1].Value,
//            ["repositoryName"] = match.Groups[2].Value
//        };
//    }

//    // Simple extraction fallback
//    var parts = request.Split('/', StringSplitOptions.RemoveEmptyEntries);
//    if (parts.Length >= 2)
//    {
//        return new Dictionary<string, object?>
//        {
//            ["owner"] = parts[0].Trim(),
//            ["repositoryName"] = parts[1].Trim()
//        };
//    }

//    // Default fallback
//    return new Dictionary<string, object?>
//    {
//        ["owner"] = "budibu85",  // Default owner
//        ["repositoryName"] = "mcp_server_dummy"  // Default repo
//    };
//}

//// Helper method to extract user parameters
//static Dictionary<string, object?> ExtractUserParameters(string request)
//{
//    // Try to extract username from common patterns
//    var words = request.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
//    // Look for GitHub username patterns
//    var potentialUsername = words.FirstOrDefault(w => 
//        !w.StartsWith("get", StringComparison.OrdinalIgnoreCase) && 
//        !w.Contains("user", StringComparison.OrdinalIgnoreCase) && 
//        !w.Contains("stars", StringComparison.OrdinalIgnoreCase) &&
//        !w.Contains("repositories", StringComparison.OrdinalIgnoreCase) &&
//        w.Length > 2);

//    return new Dictionary<string, object?>
//    {
//        ["username"] = potentialUsername ?? "budibu85"
//    };
//}

//static (string command, string[] arguments) GetCommandAndArguments(string[] args)
//{
//    return args switch
//    {
//        [var script] when script.EndsWith(".py") => ("python", args),
//        [var script] when script.EndsWith(".js") => ("node", args),
//        [var script] when Directory.Exists(script) || (File.Exists(script) && script.EndsWith(".csproj")) => ("dotnet", ["run", "--project", script, "--no-build"]),
//        _ => throw new NotSupportedException("An unsupported server script was provided. Supported scripts are .py, .js, or .csproj")
//    };
//}

static void PromptForInput()
{
    Console.WriteLine("Enter a command (or 'exit' to quit):");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("> ");
    Console.ResetColor();
}