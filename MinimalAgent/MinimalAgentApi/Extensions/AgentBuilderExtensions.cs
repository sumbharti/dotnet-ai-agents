using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace MinimalAgentApi.Extensions
{
    public static class AgentBuilderExtensions
    {
        public static void AddNetworkSupportAgentExtension(this WebApplicationBuilder builder, IChatClient chatClient)
        {
            builder.AddAIAgent(
                name: "NetworkSupportAgent",
                instructions:
                    """
                    You are a Tier 1 IT Support Agent.
                    Your answers must be concise, professional, and limited strictly to troubleshooting network and VPN connectivity.        
                    Keep responses concise — 1-2 sentences per turn. Be direct and opinionated.        
                    """,
                chatClient);
        }

        public static void AddTriageAgentExtension(this WebApplicationBuilder builder, IChatClient chatClient)
        {
            // Triage Agent - routes to specialists
            builder.AddAIAgent("TriageAgent", (sp, name) =>
            {
                return new ChatClientAgent(
                    chatClient: chatClient,
                    instructions:
                        """
                You are a routing manager.
                Analyze the customer’s request and route to the right specialist.                        
                Do not attempt to answer domain questions yourself. You are only a router.
                Keep your response to 1-2 sentences.
                """,
                    name: name,
                    description: "Routes requests to the correct specialist agent.",
                    tools: null,
                    loggerFactory: sp.GetService<ILoggerFactory>(),
                    services: sp);
            });
        }

        public static void AddOrderAgentExtension(this WebApplicationBuilder builder, IChatClient chatClient)
        {
            // Order Agent - handles order requests
            builder.AddAIAgent("OrderAgent", (sp, name) =>
            {
                return new ChatClientAgent(
                    chatClient: chatClient,
                    instructions:
                        """
            You are a logistics specialist. 
            You handle replacements, tracking, and shipping preferences.
            Keep your response to 1-2 sentences.
            """,
                    name: name,
                    description: "Handles order requests.",
                    tools: null,
                    loggerFactory: sp.GetService<ILoggerFactory>(),
                    services: sp);
            });
        }

        public static void AddRefundAgentExtension(this WebApplicationBuilder builder, IChatClient chatClient)
        {
            // Refund Agent - handles refund requests
            builder.AddAIAgent("RefundAgent", (sp, name) =>
            {
                return new ChatClientAgent(
                    chatClient: chatClient,
                    instructions:
                        """
            You are a finance specialist. 
            You look up order details, gather context, and process refund requests.
            Keep your response to 1-2 sentences.
            """,
                    name: name,
                    description: "Handles refund requests.",
                    tools: null,
                    loggerFactory: sp.GetService<ILoggerFactory>(),
                    services: sp);
            });
        }
    }
}
