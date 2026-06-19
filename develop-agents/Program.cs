

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? 
        throw new InvalidOperationException("Missing environment variable: AZURE_OPENAI_DEPLOYMENT_NAME");

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ??
        throw new InvalidOperationException("Missing environment variable: AZURE_OPENAI_ENDPOINT");

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ??
        throw new InvalidOperationException("Missing environment variable: AZURE_OPENAI_API_KEY");


Console.WriteLine("Deployment Name: " + deploymentName);
Console.WriteLine("API Endpoint: " + endpoint);

IChatClient chatClient = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential()
)
    .GetChatClient(deploymentName)
    .AsIChatClient();


var aiAgent =  chatClient.AsAIAgent(
    name: "NetworkSupportAgent",
    instructions: "You are a tier 1 support agent. Give 1 line response only."

);

var answer = await aiAgent.RunAsync("I am getting 401 error when loggin in?");

Console.WriteLine("Answer: " + answer);