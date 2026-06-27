using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;

namespace develop_agents
{
    public class DeclarativeAgents
    {
        private IChatClient chatClient;

        public DeclarativeAgents(string endpoint, string deploymentName, string? apiKey = null)
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
            // Define the agent using a YAML definition.
            var yamlDefinition =
                """
                kind: Prompt
                name: Assistant
                description: Helpful assistant
                instructions: You are a helpful assistant. You answer questions is the language specified by the user. You return your answers in a JSON format. You must include Chat as the type in your response.
                model:
                    id:gpt-5-mini
                    provider: AzureOpenAI
                    apiType: Chat
                outputSchema:
                    properties:
                        language:
                            kind: string
                            required: true
                            description: The language of the answer.
                        answer:
                            kind: string
                            required: true
                            description: The answer text.
                        type:
                            kind: string
                            required: true
                            description: The type of the response.
                """;

            // Create the agent from the YAML definition.
            var agentFactory = new ChatClientPromptAgentFactory(chatClient);
            var agent = await agentFactory.CreateFromYamlAsync(yamlDefinition);

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent!.RunAsync("Tell me a joke about a pirate in English."));
        }
    }
}
