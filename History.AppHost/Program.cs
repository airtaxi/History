var builder = DistributedApplication.CreateBuilder(args);

static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) ?? fallback;
static string RequiredEnv(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Environment variable '{name}' is required.");

var username = builder.AddParameter("username", Env("HISTORY_MONGODB_USERNAME", "admin"));
var password = builder.AddParameter("password", RequiredEnv("HISTORY_MONGODB_PASSWORD"), secret: true);

var mongodbPort = int.Parse(Env("HISTORY_MONGODB_PORT", "27017"));

var mongodb = builder.AddMongoDB("MongoDB", mongodbPort, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress()
    .WithDataBindMount(Env("HISTORY_MONGODB_DATA_MOUNT", "C:\\HistoryData"))
    .AddDatabase(Env("HISTORY_MONGODB_DATABASE", "History"));

var kakaoStoryWorkerUrl = builder.AddParameter("kakao-story-worker-url", Env("HISTORY_KAKAO_WORKER_URL", "https://kakao-story-proxy.history-kakao.workers.dev"));
var kakaoStoryWorkerSecret = builder.AddParameter("kakao-story-worker-secret", RequiredEnv("HISTORY_KAKAO_WORKER_SECRET"), secret: true);
var kakaoStoryPollIntervalSeconds = builder.AddParameter("kakao-story-poll-interval-seconds", Env("HISTORY_KAKAO_POLL_INTERVAL_SECONDS", "10"));
var kakaoStoryBatchSize = builder.AddParameter("kakao-story-batch-size", Env("HISTORY_KAKAO_BATCH_SIZE", "30"));

var api = builder.AddProject<Projects.History_ApiService>("ApiService")
    .WithReference(mongodb)
    .WithEnvironment("KakaoStoryPolling__WorkerUrl", kakaoStoryWorkerUrl)
    .WithEnvironment("KakaoStoryPolling__WorkerSecret", kakaoStoryWorkerSecret)
    .WithEnvironment("KakaoStoryPolling__PollIntervalSeconds", kakaoStoryPollIntervalSeconds)
    .WithEnvironment("KakaoStoryPolling__BatchSize", kakaoStoryBatchSize)
    .WithEnvironment("HISTORY_JWT_KEY", RequiredEnv("HISTORY_JWT_KEY"))
    .WithEnvironment("HISTORY_OAUTH_STATE_SECRET", RequiredEnv("HISTORY_OAUTH_STATE_SECRET"))
    .WithEnvironment("HISTORY_GOOGLE_CLIENT_SECRET", RequiredEnv("HISTORY_GOOGLE_CLIENT_SECRET"))
    .WithEnvironment("HISTORY_APPLE_PRIVATE_KEY_PATH", Env("HISTORY_APPLE_PRIVATE_KEY_PATH", Path.Combine(AppContext.BaseDirectory, "AuthKey_DGK52ABR8V.p8")))
    .WithEnvironment("HISTORY_FIREBASE_CREDENTIAL_PATH", Env("HISTORY_FIREBASE_CREDENTIAL_PATH", Path.Combine(AppContext.BaseDirectory, "firebaseServiceAccountKey.json")))
    .WithEnvironment("Wns__ClientId", Env("HISTORY_WNS_CLIENT_ID", ""))
    .WithEnvironment("Wns__ClientSecret", Env("HISTORY_WNS_CLIENT_SECRET", ""))
    .WithEnvironment("Wns__TokenEndpoint", Env("HISTORY_WNS_TOKEN_ENDPOINT", "https://login.microsoftonline.com/common/oauth2/v2.0/token"))
    .WithEnvironment("Wns__Scope", Env("HISTORY_WNS_SCOPE", "https://wns.windows.com/.default"))
    .WaitFor(mongodb);

var vuePort = Env("HISTORY_VUE_PORT", "5173");

builder.AddDockerfile("vue", "../History.WebFront")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(api)
    .WithContainerRuntimeArgs("-p", $"{vuePort}:80");

builder.Build().Run();
