var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "***REMOVED***", secret: true);

var mongodb = builder.AddMongoDB("MongoDB", 27017, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .WithDataBindMount("C:\\HistoryData")
    .AddDatabase("History");

var kakaoStoryWorkerUrl = builder.AddParameter("kakao-story-worker-url", "https://kakao-story-proxy.history-kakao.workers.dev");
var kakaoStoryWorkerSecret = builder.AddParameter("kakao-story-worker-secret", "***REMOVED***", secret: true);
var kakaoStoryPollIntervalSeconds = builder.AddParameter("kakao-story-poll-interval-seconds", "10");
var kakaoStoryBatchSize = builder.AddParameter("kakao-story-batch-size", "30");

var api = builder.AddProject<Projects.History_ApiService>("ApiService")
    .WithReference(mongodb)
    .WithEnvironment("KakaoStoryPolling__WorkerUrl", kakaoStoryWorkerUrl)
    .WithEnvironment("KakaoStoryPolling__WorkerSecret", kakaoStoryWorkerSecret)
    .WithEnvironment("KakaoStoryPolling__PollIntervalSeconds", kakaoStoryPollIntervalSeconds)
    .WithEnvironment("KakaoStoryPolling__BatchSize", kakaoStoryBatchSize)
    .WaitFor(mongodb);

builder.AddDockerfile("vue", "../History.WebFront")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(api)
    .WithContainerRuntimeArgs("-p", "5173:80");

builder.Build().Run();
