var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "***REMOVED***", secret: true);

var mongodb = builder.AddMongoDB("MongoDB", 27017, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .WithDataBindMount("C:\\HistoryData")
    .AddDatabase("History");

var api = builder.AddProject<Projects.History_ApiService>("ApiService")
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.AddDockerfile("vue", "../History.WebFront")
    .WaitFor(api)
    .WithHttpEndpoint(5173, 5173, "http")
    .WithEndpoint("http", endpoint => endpoint.IsExternal = true)
    .WithExternalHttpEndpoints();


builder.Build().Run();
