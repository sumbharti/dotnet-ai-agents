namespace MinimalAgentApi.Endpoints
{
    public static class RandomApi
    {
        public static RouteGroupBuilder RandomApiEndpoints(this RouteGroupBuilder app)
        {
            app.MapGet("/getExternalIp", async (IHttpClientFactory factory) =>
            {
                var httpClient = factory.CreateClient("RandomApiHttpClient");

                var httpResponseMessage = await httpClient.GetAsync("https://api.ipify.org");

                if (httpResponseMessage != null && httpResponseMessage.IsSuccessStatusCode)
                {
                    return Results.Ok(await httpResponseMessage.Content.ReadAsStringAsync());
                }

                return Results.BadRequest();
            });

            return app;
        }
    }
}