using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Extensions.AI;
using MinimalAgentApi.Endpoints;
using MinimalAgentApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. Define the variables we extracted from Microsoft Foundry
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5-mini";

// 2. Instantiate the universal chat client with OpenTelemetry GenAI instrumentation
IChatClient chatClient = new AzureOpenAIClient(
        new Uri(endpoint),
        new AzureCliCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(configure: c => c.EnableSensitiveData = true)
    .Build();

builder.Services.AddSingleton(chatClient);

// 3. Define and Register the Agents

builder.AddNetworkSupportAgentExtension(chatClient);

#region Agent Pattern with Sequential/Group Chat implementation

/////// AGENT DEFINITIONS ///////

// Triage Agent - routes to specialists
builder.AddTriageAgentExtension(chatClient);

// Order Agent - handles order requests
builder.AddOrderAgentExtension(chatClient);

// Refund Agent - handles refund requests
builder.AddRefundAgentExtension(chatClient);

// Sequential Workflow (registered as an agent for DevUI discovery)
builder.AddOrderRefundWorkflowSequential();

// Group Chat Workflow (registered as an agent for DevUI discovery)
builder.AddOrderRefundWorkflowGroupChat();

#endregion

var complianceAgent = builder.A2AServerComplianceAgent(chatClient);
complianceAgent.AddA2AServer();

// 4. Register DevUI services
builder.AddDevUI();
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Add services to the container.

var app = builder.Build();

app.MapDefaultEndpoints();

// Map DevUI endpoints 
app.MapDevUI();
app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.MapGroup("/api")
    .RandomApiEndpoints()
    .WithTags(nameof(RandomApi));

app.MapGroup("/ai")
    .AiAgentEndpoints()
    .WithTags(nameof(AiAgent));


// Expose the agent via the A2A HTTP+JSON protocol
app.MapA2AHttpJson(complianceAgent, path: "/a2a/compliance");

app.Run();
