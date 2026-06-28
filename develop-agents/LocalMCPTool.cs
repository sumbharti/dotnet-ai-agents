using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.ClientModel;

namespace develop_agents
{
    public class LocalMCPTool
    {
        private IChatClient chatClient;

        public LocalMCPTool(string endpoint, string deploymentName, string? apiKey = null)
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

        public async Task SearchDataverseWithMCP(string dataverseUrl)
        {
            await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "DataverseMCPServer",
                Command = "npx",
                Arguments = ["-y", "--verbose", "@microsoft/dataverse", "mcp", dataverseUrl]
            }));

            var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            Console.WriteLine($"[System] Discovered {mcpTools.Count()} tools from the local Dataverse MCP server.");

            AIAgent dataverseAgent = chatClient
                                .AsAIAgent(
                                    name: "DataverseInfoProvider",
                                    instructions: "You are dataverse information provider. You must only answer questions related data in dataverse. Use your tools to fetch information summarize it into professional response. Make your answer concise and to the point",
                                    tools: [.. mcpTools.Cast<AITool>()]
                                );

            string prompt = "Fetch the list of contact in dataverse environment. Return first name only";
            Console.WriteLine($"\nUser: {prompt}\n");
            Console.WriteLine("[System] Agent is searching information from Dataverse...");

            AgentResponse response = await dataverseAgent.RunAsync(prompt);
            Console.WriteLine($"\nDataverse Agent:\n{response.Text}");
        }
    }
}
