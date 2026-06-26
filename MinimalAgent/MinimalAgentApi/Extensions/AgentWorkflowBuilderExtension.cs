using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

namespace MinimalAgentApi.Extensions
{
    public static class AgentWorkflowBuilderExtension
    {
        public static void AddOrderRefundWorkflowSequential(this WebApplicationBuilder builder)
        {
            /////// SEQUENTIAL WORKFLOW (registered as an agent for DevUI discovery) ///////
            builder.AddWorkflow("OrderRefundWorkflow_Sequential", (sp, name) =>
            {
                var triageAgent = sp.GetRequiredKeyedService<AIAgent>("TriageAgent");
                var orderAgent = sp.GetRequiredKeyedService<AIAgent>("OrderAgent");
                var refundAgent = sp.GetRequiredKeyedService<AIAgent>("RefundAgent");

                var workflow = AgentWorkflowBuilder
                    .BuildSequential(name, triageAgent, orderAgent, refundAgent);

                return workflow;
            }).AddAsAIAgent();
        }

        public static void AddOrderRefundWorkflowGroupChat(this WebApplicationBuilder builder)
        {
            /////// GROUP CHAT WORKFLOW (registered as an agent for DevUI discovery) ///////
            builder.AddWorkflow("OrderRefundWorkflow_GroupChat", (sp, name) =>
            {
                var triageAgent = sp.GetRequiredKeyedService<AIAgent>("TriageAgent");
                var orderAgent = sp.GetRequiredKeyedService<AIAgent>("OrderAgent");
                var refundAgent = sp.GetRequiredKeyedService<AIAgent>("RefundAgent");

                // Group chat
                var workflow = AgentWorkflowBuilder
                    .CreateGroupChatBuilderWith(agents =>
                        new RoundRobinGroupChatManager(agents)
                        {
                            MaximumIterationCount = 3  // One turn per agent (Triage, Order, Refund)
                        })
                    .AddParticipants(triageAgent, orderAgent, refundAgent)
                    .WithName(name)
                    .WithDescription("An order and refund workflow that routes requests to the correct specialist agent and handles multi-turn conversations until resolution.")
                    .Build();

                return workflow;
            }).AddAsAIAgent();
        }
    }
}
