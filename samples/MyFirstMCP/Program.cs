using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(cfg =>
{
    cfg.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
builder.Services.AddTransient<EchoTool>();

await builder.Build().RunAsync();

[McpServerToolType]
public class EchoTool(ILogger<EchoTool> logger)
{
    [
        McpServerTool(
            Destructive = false,
            Idempotent = true,
            Name = "echo_hello",
            OpenWorld = false,
            ReadOnly = true,
            Title = "Write a hello message to the client",
            UseStructuredContent = false
        ),
        Description("Echoes the message back to the client.")
    ]
    [return: Description("The echoed message")]
    public string Echo([Description("The message to echo back")] string message)
    {
        logger.LogInformation("Echo called with message: {Message}", message);

        return $"hello, {message}!";
    }
}
