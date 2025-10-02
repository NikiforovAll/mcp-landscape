#:property JsonSerializerIsReflectionEnabledByDefault=true

#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.16.0
#:package Microsoft.Extensions.AI@9.9.1
#:package Microsoft.Agents.AI@1.0.0-preview.*
#:package Microsoft.Agents.AI.OpenAI@1.0.0-preview.*
#:package ModelContextProtocol@0.4.0-preview.1

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// Create an MCPClient for the GitHub server
await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
}));

// Retrieve the list of tools available on the GitHub server
var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
     .GetChatClient(deploymentName)
     .CreateAIAgent(instructions: "You answer questions related to GitHub repositories only.", tools: [.. mcpTools.Cast<AITool>()]);

// Invoke the agent and output the text result.
Console.WriteLine(await agent.RunAsync("Summarize the last four commits to the microsoft/semantic-kernel repository?"));
