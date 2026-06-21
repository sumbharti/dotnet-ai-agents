using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.Responses;
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace develop_agents
{
    public class AgentTools
    {
        private IChatClient chatClient;

        public AgentTools(string endpoint, string deploymentName, string? apiKey = null)
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
        }

        #region AgentTool Function call example

        public async Task FunctionCall()
        {
            AIAgent agent = chatClient
            .AsAIAgent(
                name: "LogisticsSupport",
                instructions: "You are a customer support agent. Help users track their orders concisely.",

                // We dynamically generate the AITool and pass it into the agent's capabilities
                tools: [AIFunctionFactory.Create(AgentTools.GetOrderStatus)]
            );


            Console.WriteLine($"Agent '{agent.Name}' initialized. Ready to assist.\n");

            // --- Execution Pattern 1: Synchronous (Non-Streaming) ---
            Console.WriteLine("--- Synchronous Execution ---");
            string prompt1 = "What is the status of order ORD-12345?";
            Console.WriteLine($"User: {prompt1}");

            AgentResponse response = await agent.RunAsync(prompt1);
            Console.WriteLine($"Agent: {response.Text}\n");

            // --- Execution Pattern 2: Real-Time (Streaming) ---
            Console.WriteLine("--- Streaming Execution ---");
            string prompt2 = "I need an update on ORD-99999, please.";
            Console.WriteLine($"User: {prompt2}");
            Console.Write("Agent: ");

            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt2))
            {
                Console.Write(update.Text);
            }
            Console.WriteLine("\n");

        }

        [Description("Retrieves the current shipping status of an enterprise logistics order. Invoke this tool ONLY when the user explicitly provides an Order ID.")]
        private static string GetOrderStatus(
        [Description("The exact, case-sensitive alphanumeric order identifier. Format must be 'ORD-' followed by 5 digits (e.g., ORD-12345).")] string orderId)
        {
            // Simulating a deterministic database or external API call
            if (orderId == "ORD-12345") return "IN TRANSIT - Estimated Delivery Tomorrow";
            if (orderId == "ORD-99999") return "PENDING - Awaiting Stock Validation";
            return "UNKNOWN - Order ID not found in the logistics system.";
        }

        #endregion

        #region AgentTool CodeInterpreter example

        public async Task CodeInterpretor()
        {
            // Build the tools list and add the native Code Interpreter ResponseTool via the AITool bridge extension
            List<AITool> tools = [];

            #pragma warning disable OPENAI001

            tools.Add(ResponseTool.CreateCodeInterpreterTool(
                new CodeInterpreterToolContainer(CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration([]))));

            #pragma warning restore OPENAI001

            AIAgent mathAgent = chatClient
            .AsAIAgent(
                name: "DataAnalyst",
                instructions: "You are a data analyst. You must write and execute Python code to answer complex math and data questions. Never guess the answer.",
                tools: tools
            );

            Console.WriteLine($"Agent '{mathAgent.Name}' is online with a Python sandbox.\n");

            // The agent will autonomously write a script to solve this, run it, and return the result.
            string prompt = "Determine if 17 is a Prime number or not?";
            Console.WriteLine($"User: {prompt}");

            AgentResponse response = await mathAgent.RunAsync(prompt);
            Console.WriteLine($"Agent: {response.Text}");
        }

        #endregion

        #region AgentTool Manual Intervention example

        public async Task ManualInterpretor()
        {
            AIFunction rawRefundFunction = AIFunctionFactory.Create(AgentTools.IssueRefund);
            AIFunction secureRefundTool = new ApprovalRequiredAIFunction(rawRefundFunction);

            AIAgent agent = chatClient
           .AsAIAgent(
               name: "FinanceSupport",
               instructions: "You are a customer support agent with billing privileges. You must help users process refunds.",
               tools: [secureRefundTool]
           );

            // We must use a session/thread so the agent remembers the context after the human pauses it
            AgentSession session = await agent.CreateSessionAsync();

            string userPrompt = "I was charged twice for order ORD-99999. Please issue a refund for $45.50.";
            Console.WriteLine($"User: {userPrompt}");

            // 4. Execute the Agent (First Pass)
            AgentResponse response = await agent.RunAsync(userPrompt, session);

            // 5. Check if the Agent paused to request human approval
            var approvalRequests = response.Messages
                .SelectMany(x => x.Contents)
                .OfType<ToolApprovalRequestContent>()
                .ToList();

            if (approvalRequests.Any())
            {
                ToolApprovalRequestContent request = approvalRequests.First();

                var requestToolCall = (FunctionCallContent)request.ToolCall;
                string toolName = requestToolCall.Name;
                string toolArguments = JsonSerializer.Serialize(requestToolCall.Arguments);

                // Display the AI's intent to the human manager
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[SECURITY ALERT] Agent requests permission to execute '{toolName}'");
                Console.WriteLine($"Proposed Arguments: {toolArguments}");
                Console.Write("Do you approve this action? [Y/N]: ");
                Console.ResetColor();

                string? input = Console.ReadLine();
                bool isApproved = input?.Trim().ToUpper() == "Y";

                // 6. Send the human's decision back to the Agent to resume execution
                var approvalMessage = new ChatMessage(
                    ChatRole.User,
                    new[] { request.CreateResponse(isApproved) }
                );

                response = await agent.RunAsync(approvalMessage, session);
            }

            // 7. Print the final synthesis
            Console.WriteLine($"\nAgent: {response.Text}");
        }

        [Description("Issues a financial refund to a customer. Use this ONLY when the user explicitly requests a refund and provides an Order ID.")]
        private static string IssueRefund(
        [Description("The Order ID to refund (e.g., ORD-12345).")] string orderId,
        [Description("The decimal amount to refund.")] decimal amount)
        {
            // Simulating a deterministic call to a payment gateway (e.g., Stripe or PayPal)
            Console.WriteLine($"\n[SYSTEM LOG] Executing secure transaction: Refunded ${amount} to {orderId}.\n");
            return $"SUCCESS: ${amount} has been refunded to order {orderId}.";
        }

        #endregion
    }
}
