var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("admin");
var password = builder.AddParameter("7742516665ff3ea9a9011c3645119f15baefc947d1a5ae09cdebc791d0148af2", secret: true);

var mongodb = builder.AddMongoDB("MongoDB", 27017, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .AddDatabase("History");

builder.AddProject<Projects.History_ApiService>("ApiService")
    .WithReference(mongodb)
    .WithHttpEndpoint()
    .WaitFor(mongodb);

builder.Build().Run();
