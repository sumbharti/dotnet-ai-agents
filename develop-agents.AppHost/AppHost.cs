var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MinimalAgentApi>("minimalagentapi");

builder.Build().Run();
