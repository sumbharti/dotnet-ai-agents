using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;

namespace develop_agents
{
    public class AgenticSkills
    {
        private IChatClient chatClient;

        public AgenticSkills(string endpoint, string deploymentName, string? apiKey = null)
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

        public async Task Execute()
        {
            // Discover skills from the 'skills' directory

#pragma warning disable MAAI001

            var skillsProvider = new AgentSkillsProvider(
                Path.Combine(AppContext.BaseDirectory, "skills"));

#pragma warning restore MAAI001 

            AIAgent agent = chatClient
                            .AsAIAgent(new ChatClientAgentOptions
                            {
                                Name = "UnitConverterAgent",
                                ChatOptions = new()
                                {
                                    Instructions = "You are a helpful assistant that can convert units.",
                                },
                                AIContextProviders = [skillsProvider],
                            });

            // --- Example: Unit conversion ---
            Console.WriteLine("Converting units with file-based skills");
            Console.WriteLine(new string('-', 60));

            AgentResponse response = await agent.RunAsync(
                "How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

            Console.WriteLine($"Agent: {response.Text}");
        }
    }
}
