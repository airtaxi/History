using DotNet.RateLimiter;
using DotNet.RateLimiter.Extensions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using History.ApiService;
using History.ApiService.Services;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.Enums;
using History.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text;
using System.Text.Json.Serialization;

var firebaseServiceAccountKeyJsonPath = Path.Combine(AppContext.BaseDirectory, "firebaseServiceAccountKey.json");
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(firebaseServiceAccountKeyJsonPath)
});

BsonSerializer.RegisterSerializer(new EnumSerializer<DiscoveryOption>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<PostReactionType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<FriendshipStatus>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<NotificationType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<PushNotificationType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<MediaBucket>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<Rank>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<ErrorType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<SocialService>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<RestrictionType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<ReportType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<ReportTarget>(BsonType.String));

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://history.cenox.io")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.AddMongoDBClient(connectionName: "History");


// Services
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddHostedService<DatabaseInitService>();
builder.Services.AddHostedService<BirthdayService>();
builder.Services.AddRateLimitService(builder.Configuration);

// Unlock the file upload size limit.
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Unlock the request body size limit for Kestrel server.
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});

// Add controllers to the container.
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApiDocument(config =>
{
    config.SchemaSettings.GenerateEnumMappingDescription = true;
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Constants.JwtIssuer,
            ValidAudience = Constants.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.JwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenType = context.Principal.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
                if (tokenType != "access") context.Fail("Invalid token type");

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Must be called first
app.UseForwardedHeaders();

app.UseCors();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.UseOpenApi();
app.UseSwaggerUi();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", MainPageHandler)
.WithRateLimiter(options =>
{
    options.PeriodInSec = 1;
    options.Limit = 1;
});


app.Run();

static async Task MainPageHandler(HttpContext context) => await context.Response.WriteAsync("It Works!");