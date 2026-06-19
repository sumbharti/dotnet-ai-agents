

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;

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

// using api key
//IChatClient chatClient = new AzureOpenAIClient(
//    new Uri(endpoint),
//    new ApiKeyCredential(apiKey)
//)
//    .GetChatClient(deploymentName)
//    .AsIChatClient();


var friendlyChatAgent = chatClient.AsAIAgent(
    name: "FriendlyChatAgent",
    instructions: "You are a friendly assistant. Make sure answers very short."
);

var query = "what is the capital of india?";

var answer = await friendlyChatAgent.RunAsync(query);

Console.WriteLine("Answer: " + answer);