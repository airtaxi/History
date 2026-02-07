var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "a742516665ff3ea9a9011c3645119f15baefc947d1a5ae09cdebc791d0148af2", secret: true);

var mongodb = builder.AddMongoDB("MongoDB", 27017, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .WithDataBindMount("C:\\HistoryData")
    .AddDatabase("History");

var api = builder.AddProject<Projects.History_ApiService>("ApiService")
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.AddDockerfile("vue", "../History.WebFront")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(api)
    .WithContainerRuntimeArgs("-p", "5173:80");

builder.Build().Run();
