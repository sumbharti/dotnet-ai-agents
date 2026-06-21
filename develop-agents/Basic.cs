using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Text.Json.Serialization;

namespace develop_agents
{
    public class Basic
    {
        private IChatClient chatClient;

        public Basic(string endpoint, string deploymentName, string? apiKey = null)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // using api key
                chatClient = new AzureOpenAIClient(
                    new Uri(endpoint),
                    new ApiKeyCredential(apiKey)
                )
                .GetChatClient(deploymentName)
                .AsIChatClient();
            }
            else
            {
                chatClient = new AzureOpenAIClient(
                    new Uri(endpoint),
                    new AzureCliCredential()
                )
                .GetChatClient(deploymentName)
                .AsIChatClient();
            }
        }
        
        internal async Task StructuredResponse()
        {
            var friendlyChatAgent = chatClient.AsAIAgent(
                name: "FriendlyChatAgent",
                instructions: "You are a friendly assistant. Make sure answers very concise."
            );


            AgentSession session = await friendlyChatAgent.CreateSessionAsync();

            while (true)
            {
                Console.Write("> ");

                var query = Console.ReadLine();

                var answer = await friendlyChatAgent.RunAsync<AnalysisResponse>(query!, session);

                Console.WriteLine("Answer: " + answer);

            }
        }
    }

    // response contract
    public record AnalysisResponse
    (
        [property: JsonPropertyName("question")]
    string Question,

        [property: JsonPropertyName("response")]
    string Response
    );
}
