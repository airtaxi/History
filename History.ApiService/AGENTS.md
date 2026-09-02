# History.ApiService Copilot Guidelines

## Project Overview

History.ApiService is the backend of the "History" social media application — a .NET ASP.NET Core Web API project. It uses MongoDB as the database, implements JWT authentication, and integrates Firebase for push notifications and Google/Apple OAuth login.

## Architecture

- **Framework**: .NET 10, ASP.NET Core
- **Database**: MongoDB via the official MongoDB.Driver
- **Authentication**: JWT Bearer tokens, Firebase, Apple/Google OAuth
- **Pattern**: Controller -> Service -> Repository (MongoDB collections)
- **Dependency Injection**: Services registered by interface in Program.cs
- **Result Pattern**: `Result<T>` and `Result` types for operation outcomes

## Key Components

- **Controllers**: REST API endpoints using standard HTTP status codes
- **Services**: Business logic implementing interfaces
- **DataTypes**: Request/response DTOs and internal data structures
- **Helpers**: Utility classes for external integrations (Apple OAuth, media processing)
- **Enums/Constants**: Defined in History.Commons

## Controllers

The API exposes the following controllers, each handling a specific domain:

- **PostController**: Post CRUD, timeline/public posts, reactions, reposts, shares, discovery options, search, external URL content filling, polls, bookmarks
- **GoogleController**: Google OAuth flow (login URL generation, callback handling)
- **ReportController**: Report record management (create, view, delete) with moderator access
- **CommentController**: Comment CRUD, likes, access control based on post permissions
- **MediaController**: Media file serving with caching headers, special birthday media handling
- **FriendshipController**: Friend request management (send/accept/reject/cancel), block/ignore, favorite friends, friend list search
- **UserController**: User registration/login via OAuth, profile management (nickname, description, media), JWT refresh, user search, notifications, moderator features
- **AppleController**: Apple OAuth flow, JWT token generation
- **ModerationController**: Post/comment deletion by moderators, moderation history search
- **MessageController**: Private messaging (send, search, read status, permission checks)
- **StickerController**: Sticker CRUD (create, view, search, delete), sticker asset management, subscribe/unsubscribe, recent usage history

All controllers use rate limiting, proper authorization, and consistent error handling patterns.

## Services

Services implement business logic and are abstracted through interfaces. Key services:

- **UserService**: User CRUD, profile updates (nickname, description, birthday, media), search permission settings, handle management, memos, push notification permissions, message receive permissions, account deletion
- **MediaService**: Media upload/transformation/storage (GridFS), thumbnail generation, file deletion, per-user media management
- **FriendshipService**: Friend request/accept/reject/cancel, block/ignore, friend list search, relationship checks, favorite friends management
- **PostService**: Post CRUD, timeline/public post retrieval, reactions/reposts, discovery option changes, access control, search, external URL filling, poll voting (PollVote management), bookmarks
- **CommentService**: Comment CRUD, likes, access control, response DTO creation
- **NotificationService**: Push notification send/delete, Firebase token management, notification filtering
- **MessageService**: Message send/search/read handling, permission checks, response DTO creation
- **ReportService**: Report creation/processing/deletion, report history search
- **ModerationService**: Post/comment deletion, moderation history management
- **RefreshTokenService**: JWT refresh token management
- **BirthdayService**: Birthday notification handling (hosted service)
- **StickerService**: Sticker CRUD, sticker asset management, sticker search, unofficial sticker access control, subscribe/unsubscribe, recent usage history

Services use the Result pattern; all database operations are async/await.

## Coding Standards

### Naming Conventions

- Classes: PascalCase
- Methods: PascalCase
- Properties: PascalCase
- Private fields: _camelCase (underscore prefix)
- Interfaces: IPascalCase
- Enums: PascalCase

### Code Style

- Implicit usings enabled (`ImplicitUsings`)
- Nullable reference types disabled (`Nullable` disable)
- C# 12 features allowed (`LangVersion` preview)
- async/await for all I/O operations
- LINQ for data manipulation
- String interpolation `$"{variable}"`

### Error Handling

- `Result<T>` in service methods
- Controllers map `Result.Error` to appropriate HTTP status codes
- Log errors but never expose internal details to the client

### Database Operations

- Typed collections with MongoDB.Driver
- Builders for complex queries
- async for all operations
- Enums serialized as strings, configured in Program.cs

### Delete Logic Sync Rules

- `PostService.DeletePostAsync(...)` is the single-delete path; `PostService.DeletePostsAsync(...)` is the bulk (batch) delete path.
- When modifying the single-delete logic (`DeletePostAsync`), **must** also update the bulk delete logic (`DeletePostsAsync`) so that the cleanup scope (media/notifications/reports/bookmarks, etc.) stays consistent between the two.

### Security

- JWT validation in middleware
- User claims accessed via `User.FindFirst(ClaimTypes.NameIdentifier)`
- Access control checks in services
- Rate limiting with DotNet.RateLimiter

### Media Handling

- Media upload via form data
- Processed and stored with unique IDs
- Thumbnails generated for videos/images
- MIME type validation
- URL can be built from MediaId alone: /api/{mediaId}

### Notifications

- Push notifications via Firebase Cloud Messaging
- Notification types defined as enums
- Sent asynchronously after operations

### Validation

- Input sanitization in Utils.cs
- Business rule validation in services
- Length limits and content checks

## Common Patterns

### Service Method Structure

```csharp
public async Task<Result<T>> MethodNameAsync(params)
{
    // validation
    if (invalid) return (ErrorType.BadRequest, "message");

    // database operations
    var data = await _collection.Find(filter).ToListAsync();

    // business logic
    // ...

    return result;
}
```

### Controller Method Structure

```csharp
[HttpVerb("route")]
[ProducesResponseType<T>(200)] // T: result.Value type
[ProducesResponseType<string>(400)] // map all possible errors
public async Task<IActionResult> MethodName(params)
{
    var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (requesterId == null) return Unauthorized();

    var result = await _service.MethodAsync(params);
    if (result.IsSuccess) return Ok(result.Value);
    else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
    // ... map all other possible errors
    else return StatusCode(500, result.FullErrorMessage);
}
```

### MongoDB Queries

- Builders for filters, updates, sorts
- Project only needed fields for performance
- Pagination via fromPostId and CreatedAt comparisons

### Access Control

- Friend relationship checks for privacy settings
- Moderators (Rank >= Moderator) bypass restrictions
- Permission checks via CheckAccessAsync

## Dependencies

- Microsoft.AspNetCore.* for web framework
- MongoDB.Driver for database
- FirebaseAdmin for notifications
- System.IdentityModel.Tokens.Jwt for JWT
- RestSharp for HTTP client
- BouncyCastle for cryptography

## Testing

- Unit tests mock services and database
- Integration tests cover full API flows
- Test MongoDB instance used

## Deployment

- Docker/Aspire configuration
- Environment-specific settings in appsettings.json
- Firebase service account key required
- MongoDB connection string configuration required

## Important Notes

- Sanitize all text content for security
- Media IDs are GUID strings
- Use DateTime.UtcNow for timestamps
- Korean error messages for user-facing responses
- Rate limiting to prevent abuse
- CORS configured for specific origins
- Update this AGENTS.md when adding new controllers or services to document new components