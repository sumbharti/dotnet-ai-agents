using Microsoft.Agents.AI;

namespace MinimalAgentApi.Endpoints
{
    public static class AiAgent
    {
        public static RouteGroupBuilder AiAgentEndpoints(this RouteGroupBuilder app)
        {
            app.MapPost("/chat", async (ChatRequest request,
                [FromKeyedServices("NetworkSupportAgent")] AIAgent networkSupportAgent
                ) =>
            {
                var response = await networkSupportAgent.RunAsync(request.Message);
                return Results.Ok(new { response = response.Text });
            });

            return app;
        }

        record ChatRequest(string Message);
    }
}
