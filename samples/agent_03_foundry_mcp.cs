#:property JsonSerializerIsReflectionEnabledByDefault=true

#:package Azure.AI.Agents.Persistent@1.2.0-beta.6
#:package Azure.Identity@1.16.0
#:package Microsoft.Agents.AI@1.0.0-preview.*
#:package Microsoft.Agents.AI.AzureAI@1.0.0-preview.*
#:package Spectre.Console@0.51.1

using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Spectre.Console;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_FOUNDRY_PROJECT_ENDPOINT is not set.");
var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_MODEL_ID") ?? "gpt-4.1-mini";

const string AgentName = "MicrosoftLearnAgent";
const string AgentInstructions =
    "You answer questions by searching the Microsoft Learn content only.";

// Get a client to create/retrieve server side agents with.
var persistentAgentsClient = new PersistentAgentsClient(endpoint, new AzureCliCredential());

// Create an MCP tool definition that the agent can use.
var mcpTool = new MCPToolDefinition(
    serverLabel: "microsoft_learn",
    serverUrl: "https://learn.microsoft.com/api/mcp"
);
mcpTool.AllowedTools.Add("microsoft_docs_search");

// Create a server side persistent agent with the Azure.AI.Agents.Persistent SDK.
var agentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
    model: model,
    name: AgentName,
    instructions: AgentInstructions,
    tools: [mcpTool]
);

// Retrieve an already created server side persistent agent as an AIAgent.
AIAgent agent = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);

Console.WriteLine($"Agent '{agent.Name}' is ready to use.");

// Create run options to configure the agent invocation.
var runOptions = new ChatClientAgentRunOptions()
{
    ChatOptions = new()
    {
        RawRepresentationFactory = (_) =>
            new ThreadAndRunOptions()
            {
                ToolResources = new MCPToolResource(serverLabel: "microsoft_learn")
                {
                    RequireApproval = new MCPApproval("never"),
                }.ToToolResources(),
            },
    },
};

// You can then invoke the agent like any other AIAgent.
AgentThread thread = agent.GetNewThread();

var response = await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Agent is thinking...", async ctx =>
    {
        return await agent.RunAsync(
            """
            Please find what's new in .NET 10.

            Hint: Use the 'microsoft_docs_search' tool.
            """,
            thread,
            runOptions
        );
    });

Console.WriteLine(response);

// Cleanup for sample purposes.
await persistentAgentsClient.Administration.DeleteAgentAsync(agent.Id);
