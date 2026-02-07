var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "***REMOVED***", secret: true);

var mongodb = builder.AddMongoDB("MongoDB", 27017, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .WithDataBindMount("C:\\HistoryData")
    .AddDatabase("History");

var api = builder.AddDockerfile("ApiService", "../", "History.ApiService/Dockerfile")
    .WithReference(mongodb)
    .WaitFor(mongodb)
    .WithContainerRuntimeArgs("-p", "31227:31227");

builder.AddDockerfile("vue", "../History.WebFront")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(api)
    .WithContainerRuntimeArgs("-p", "5173:80");

builder.Build().Run();
