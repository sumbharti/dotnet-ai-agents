using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace MinimalAgentApi.Extensions
{
    public static class A2AServerAgent
    {
        public static IHostedAgentBuilder A2AServerComplianceAgent(this WebApplicationBuilder builder, IChatClient chatClient)
        {
            
            var complianceAgent = builder.AddAIAgent(
                name: "compliance",
                instructions: "You are a strict enterprise compliance auditor. Review the provided text for GDPR violations. Be concise and authoritative."
            );

            return complianceAgent;
        }
    }
}
