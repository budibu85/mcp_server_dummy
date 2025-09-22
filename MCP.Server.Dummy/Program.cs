using MCP.Server.Dummy.Services;
using MCP_Server_Local.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;

var builder = Host.CreateApplicationBuilder(args);


// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Register HttpClient for GitHub API
builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "MCP-Server-Dummy/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("Authorization", $"token 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");

});

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GitHubTools>();

await builder.Build().RunAsync();
