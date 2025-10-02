#:property JsonSerializerIsReflectionEnabledByDefault=true

#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.16.0
#:package Microsoft.Extensions.AI@9.9.1
#:package Microsoft.Agents.AI@1.0.0-preview.*
#:package Microsoft.Agents.AI.OpenAI@1.0.0-preview.*

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;

var endpoint =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName =
    Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

const string JokerName = "Joker";
const string JokerInstructions = "You are good at telling jokes.";

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName)
    .CreateAIAgent(JokerInstructions, JokerName);

// Invoke the agent and output the text result.
Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));
