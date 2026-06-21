using develop_agents;

var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? 
        throw new InvalidOperationException("Missing environment variable: AZURE_OPENAI_DEPLOYMENT_NAME");

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ??
        throw new InvalidOperationException("Missing environment variable: AZURE_OPENAI_ENDPOINT");

var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ??
        throw new InvalidOperationException("Missing environment variable: AZURE_OPENAI_API_KEY");

Console.WriteLine("Endpoint: " + endpoint);
Console.WriteLine("Deployment Name: " + deploymentName);

AgentTools agentTools = new AgentTools(endpoint, deploymentName);
agentTools.FunctionCall().Wait();

Console.WriteLine("Press any key to exit...");