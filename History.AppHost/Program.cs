var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "***REMOVED***", secret: true);

var mongodb = builder.AddMongoDB("MongoDB", 27017, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .AddDatabase("History");

builder.AddProject<Projects.History_ApiService>("ApiService")
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.Build().Run();
