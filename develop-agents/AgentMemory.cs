using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Text.Json;

namespace develop_agents
{
    public class AgentMemory
    {
        private IChatClient chatClient;
        private ISessionRepository sessionRepository;

        public AgentMemory(string endpoint, string deploymentName, string? apiKey = null)
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

            chatClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureCliCredential()
            )
            .GetChatClient(deploymentName)
            .AsIChatClient();


            sessionRepository = new MockCosmosDbRepository();
        }

        #region InMemory AgentSession basic example

        public async Task InMemoryAgentSession()
        {
            var agent = chatClient.AsAIAgent(
                name: "StatelessAgent",
                instructions: "You are a friendly agent. You do not remember previous conversations. Keep your answers consice and to the point."
            );

            AgentSession session = await agent.CreateSessionAsync();

            while (true)
            {
                Console.Write("> ");
                var query = Console.ReadLine();
                var answer = await agent.RunAsync(query!, session);
                Console.WriteLine("Answer: " + answer);
            }


            // This conversation is volatile and will be lost as soon as the application exists. If you want to persist the conversation, you can implement a custom IAgentSessionRepository and pass it to the CreateSessionAsync method.
        }

        #endregion

        #region PersistAgentSession, chat session can be stored in a database (Cosmos db, SQL Server, etc.). 
        // Here a mock implementation is provided for demonstration purposes using an in-memory dictionary.

        public interface ISessionRepository
        {
            Task<string?> GetSessionJsonAsync(string sessionId);
            Task SaveSessionJsonAsync(string sessionId, string jsonPayload);
        }

        // (Mock implementation of a database like Cosmos DB or SQL Server)
        public class MockCosmosDbRepository : ISessionRepository
        {
            private readonly Dictionary<string, string> _datastore = new();

            public Task<string?> GetSessionJsonAsync(string sessionId) =>
                Task.FromResult(_datastore.TryGetValue(sessionId, out var json) ? json : null);

            public Task SaveSessionJsonAsync(string sessionId, string jsonPayload)
            {
                _datastore[sessionId] = jsonPayload;
                return Task.CompletedTask;
            }
        }

        private async Task<string> statelessAgentService(string sessionId, string userMessage)
        {
            AgentSession session;

            var agent = chatClient.AsAIAgent(
                name: "StatelessAgent",
                instructions: "You are a friendly agent. You do not remember previous conversations. Keep your answers consice and to the point."
            );

            string? savedSessionJson = await sessionRepository.GetSessionJsonAsync(sessionId);

            if (!string.IsNullOrEmpty(savedSessionJson))
            {
                using JsonDocument doc = JsonDocument.Parse(savedSessionJson);

                // Step B: Deserialize the session, restoring the agent's memory
                session = await agent.DeserializeSessionAsync(doc.RootElement);
                Console.WriteLine($"[SYSTEM LOG] Successfully restored session {sessionId} from database.");
            }
            else
            {
                // Step C: Fallback - Create a brand new session if no history exists
                session = await agent.CreateSessionAsync();
                Console.WriteLine($"[SYSTEM LOG] Created new session for {sessionId}.");
            }

            // Step D: Execute the agent with the loaded session
            AgentResponse response = await agent.RunAsync(userMessage, session);

            // Step E: Serialize the newly updated session state
            JsonElement updatedSessionElement = await agent.SerializeSessionAsync(session);
            string updatedJsonString = JsonSerializer.Serialize(updatedSessionElement);

            // Step F: Persist the updated state back to the database
            await sessionRepository.SaveSessionJsonAsync(sessionId, updatedJsonString);

            return response.Text;
        }

        public async Task PersistAgentSession()
        {
            string userId = "user-778899";

            Console.WriteLine("--- Monday Morning ---");
            string response1 = await statelessAgentService(userId, "Hi, I am planning a road trip for my birthday.");
            Console.WriteLine($"Agent: {response1}\n");

            // The application could completely shut down or restart here.
            // The memory is safely stored in the repository.

            Console.WriteLine("--- Friday Afternoon (Simulating a new server request) ---");
            string response2 = await statelessAgentService(userId, "Do you remember what I said I was planning?");
            Console.WriteLine($"Agent: {response2}\n");
        }

        #endregion
}
}
