#:property JsonSerializerIsReflectionEnabledByDefault=true

#:package Azure.AI.Agents.Persistent@1.2.0-beta.8
#:package Azure.Identity@1.17.0
#:package Spectre.Console@0.51.1

using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Spectre.Console;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_FOUNDRY_PROJECT_ENDPOINT is not set.");
var model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_MODEL_ID") ?? "gpt-4.1-mini";

const string AgentName = "MicrosoftLearnAgent";
const string AgentInstructions =
    "You answer questions by searching the Microsoft Learn content only.";

// Get a client to create/retrieve server side agents with.
var client = new PersistentAgentsClient(endpoint, new AzureCliCredential());

// Create an MCP tool definition that the agent can use.
var mcpTool = new MCPToolDefinition(
    serverLabel: "microsoft_learn",
    serverUrl: "https://learn.microsoft.com/api/mcp"
);
mcpTool.AllowedTools.Add("microsoft_docs_search");

// Create a server side persistent agent with the Azure.AI.Agents.Persistent SDK.
PersistentAgent agent = await client.Administration.CreateAgentAsync(
    model: model,
    name: AgentName,
    instructions: AgentInstructions,
    tools: [mcpTool]
);

Console.WriteLine($"Agent '{agent.Name}' is ready to use.");

// Create a thread for conversation
PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

// Create the message
await client.Messages.CreateMessageAsync(
    thread.Id,
    MessageRole.User,
    """
    Please find what's new in .NET 10.

    Hint: Use the 'microsoft_docs_search' tool.
    """
);

// Create MCP tool resources with approval settings
var mcpToolResource = new MCPToolResource(serverLabel: "microsoft_learn")
{
    RequireApproval = new MCPApproval("never"),
};

// Run the agent and wait for completion
ThreadRun run = null!;
await AnsiConsole
    .Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync(
        "Agent is thinking...",
        async ctx =>
        {
            run = await client.Runs.CreateRunAsync(
                thread,
                agent,
                toolResources: mcpToolResource.ToToolResources()
            );

            do
            {
                await Task.Delay(500);
                run = await client.Runs.GetRunAsync(thread.Id, run.Id);
            } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
        }
    );

// Get the messages
Pageable<PersistentThreadMessage> messages = client.Messages.GetMessages(
    threadId: thread.Id,
    order: ListSortOrder.Ascending
);

foreach (PersistentThreadMessage threadMessage in messages)
{
    if (threadMessage.Role == MessageRole.Agent)
    {
        foreach (MessageContent content in threadMessage.ContentItems)
        {
            if (content is MessageTextContent textContent)
            {
                Console.WriteLine(textContent.Text);
            }
        }
    }
}

// Cleanup for sample purposes.
await client.Threads.DeleteThreadAsync(thread.Id);
await client.Administration.DeleteAgentAsync(agent.Id);
