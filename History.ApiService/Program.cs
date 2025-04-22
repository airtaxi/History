using History.ApiService;
using History.ApiService.Services;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.Enums;
using History.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Text;

BsonSerializer.RegisterSerializer(new EnumSerializer<DiscoveryOption>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<PostReactionType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<FriendshipStatus>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<MediaBucket>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<Rank>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<ErrorType>(BsonType.String));
BsonSerializer.RegisterSerializer(new EnumSerializer<SocialService>(BsonType.String));

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddMongoDBClient(connectionName: "History");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Services
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddHostedService<DatabaseInitService>();

// Add controllers to the container.
builder.Services.AddControllers();
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
                var token = context.SecurityToken as JwtSecurityToken;
                var tokenType = token?.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;

                if (tokenType != "access")
                {
                    context.Fail("Invalid token type");
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", MainPageHandler);

app.Run();

static async Task MainPageHandler(HttpContext context) => await context.Response.WriteAsync("It Works!");