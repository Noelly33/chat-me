# AGENTS.md — SideCar.Auth.Api

## Stack
- .NET 10 Web API (`<TargetFramework>net10.0</TargetFramework>`). Requires .NET 10 SDK.
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` against PostgreSQL.
- JWT bearer auth (HS256, `SymmetricSecurityKey`).
- AutoMapper 16, FluentValidation 12.
- Single project, single solution. The solution uses the new XML `.slnx` format (`SideCar.Auth.Api.slnx`); older SDKs will not open it.

## Layout
All code lives under `SideCar.Auth.Api/`. Hand-rolled clean-ish architecture:

- `Domain/Model` — EF entities (`Usuario`, `RefreshToken`) with explicit `[Table]` / `[Column]` mappings.
- `Domain/Repositories`, `Domain/Services` — interfaces only (`IAuthRepository`, `ITokenRepository`, `IAuthService`, `ITokenService`).
- `Application/Services` — concrete services (`AuthService`, `TokenService`). DI-registered as `Scoped` in `Program.cs`.
- `InfraStructure/Context/AuthContext.cs` — `DbSet<Usuario>`, `DbSet<RefreshToken>`. No fluent configuration in `OnModelCreating`.
- `InfraStructure/Repositories` — EF implementations.
- `InfraStructure/mappers/MappersProfile.cs` — AutoMapper profile.
- `InfraStructure/Validators` — `AbstractValidator<T>` classes.
- `InfraStructure/Utils.cs` — `getGuidUserByExpiredToken`, used by the refresh flow.
- `Controllers/AuthController.cs` — the only real controller, route `api/v1/auth`.
- `DTOS` — DTOs plus `MsRequest<T>` / `MsResponse<T>` wrappers (see below).
- `Migrations` — one committed migration `20260613040354_AgregarTablaUsuarios` + `AuthContextModelSnapshot.cs`.

`WeatherForecast.cs` and `Controllers/WeatherForecastController.cs` are leftover `dotnet new webapi` scaffold. **Do not add endpoints there.** Delete them only as an intentional cleanup commit.

## Commands
Run from `SideCar.Auth.Api/` unless noted.

- Build: `dotnet build`
- Run (http, port 5016): `dotnet run --launch-profile http`
- Run (https, ports 7101 + 5016): `dotnet run --launch-profile https`
- Test register endpoint: `SideCar.Auth.Api.http` (VS / Rider HTTP client) or `curl http://localhost:5016/api/v1/auth/register`.
- Add EF migration: `dotnet ef migrations add <Name>` (install once with `dotnet tool install --global dotnet-ef`).
- Apply migrations: `dotnet ef database update`.

## Configuration
`Program.cs` reads:
- `ConnectionStrings:PostgresConnection`
- `JwtSettings:Secret`, `JwtSettings:Issuer`, `JwtSettings:Audience`, `JwtSettings:AccessTokenExpirationInMinutes`, `JwtSettings:RefreshTokenExpirationInDays`

`appsettings.json` only carries logging + `AllowedHosts`. Real values (Postgres host, DB password, JWT secret, lifetimes) are in `appsettings.Development.json` and **are checked into the repo**. Treat that file as a leak:

- Do not add more secrets there. For non-dev environments, override via env vars (`ConnectionStrings__PostgresConnection`, `JwtSettings__Secret`, ...) or a real secret store.
- The hardcoded DB host (`150.136.56.67`), DB password, and JWT secret are placeholders. Rotate them before any non-local use and never paste real ones into git.

## API contract
Every endpoint expects a JSON body shaped like `MsRequest<T>` (`{ Header: { TransactionId, Timestamp, Device }, Data: <T> }`) and returns `MsResponse<T>`. See `DTOS/MsRequest.cs` and `DTOS/MsResponse.cs`.

Currently exposed (all under `api/v1/auth`):

- `POST /register` — runs `RegisterUserValidator` (FluentValidation, including a DB-backed email-uniqueness check via `IAuthRepository.GetUserByEmail`), then `IAuthService.register`, returns access + refresh tokens.
- `POST /login` — takes `MsRequest<LoginUserDTO>` with `Identifier` (email OR `NombreUsuario`) and `Password`. `AuthService.Login` looks up by email first, then username, then `PasswordHasher<Usuario>.VerifyHashedPassword` validates the hash; on miss it throws `UnauthorizedAccessException` and the controller returns `401` with a generic "Credenciales inválidas" message (no user-enumeration leak). On success it reuses `ITokenService.GenerarTokens` and returns the same shape as register (`LoginResultDTO`: email, access token, refresh token, expiration).
- `POST /refresh` — takes `{ Token (expired access), RefreshToken }` in `Data`. `TokenService.ResfreshToken` (note the typo) revokes the old refresh token, then issues a new access + refresh pair (rotation). Throws `SecurityTokenException` on bad/expired/mismatched tokens; controller maps to `401`.
- `POST /validate` — takes `{ Token }` in `Data`. Stateless, no DB hit: validates JWT signature + lifetime + issuer + audience, returns `{ valid, userId, username, email, expiresAt, reason? }`. Designed to be called from nginx `auth_request` and from the upstream microservices; `200` for valid, `401` for invalid.
- `PUT /profile` — `[Authorize]`. Updates the authenticated user's profile. `userId` is read from the `NameIdentifier` claim; the body is partial (any field may be `null` to keep current value). Returns the updated user.

`TokenService.ResfreshToken` (typo is intentional and preserved from the original signature) implements token rotation: the old refresh token is marked `EstaRevocado` and a fresh pair is issued in the same `SaveChanges`. Reuse of a revoked refresh token now fails instead of silently re-issuing.

## Conventions / gotchas
- **Namespace casing is inconsistent** (`InfraStructure`, `mappers`, `Validators`, `DTOS`). Match the existing casing in each folder when adding files; do not rename folders as a drive-by.
- `Program.cs` sets `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);` — leave it.
- `builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();` picks up any new `AbstractValidator<T>` in the same assembly automatically. Do not add per-validator `AddScoped` lines.
- `AuthRepository` exposes one `Add`/`Update`/... method plus a separate `Save()`; callers do both. Match that style for new repo methods rather than wrapping them in a single `AddAndSaveAsync`.
- `Domain/Model/Usuario.CreadoAt` defaults to `DateTime.UtcNow` client-side; it is not a database computed column.
- No test project, no `.editorconfig`, no `global.json`, no `.github/` CI, no README. If you add a test project, put it as a sibling under the repo root (e.g. `SideCar.Auth.Api.Tests/`) and reference it from `SideCar.Auth.Api.slnx`.

## Common footguns
- `Microsoft.AspNetCore.OpenApi` is referenced (for `MapOpenApi()` in dev) but `Swashbuckle` is not. Do not try to use Swagger UI without adding it.
- `JwtBearer` is wired in `Program.cs` (`AddAuthentication().AddJwtBearer(...)` + `app.UseAuthentication()`) and the secret/issuer/audience come from `JwtSettings`. Missing or empty `JwtSettings:Secret` at startup throws `InvalidOperationException` and the app refuses to start — do not weaken that check.
- `TokenService.ValidateToken` is intentionally stateless (no DB call) so nginx `auth_request` is cheap. Tradeoff: a token for a deleted user stays valid until expiry. If you need stronger guarantees, add a `GetUserById` check in `AuthService` and call it from a separate endpoint.
- The `update` flow uses EF change tracking: `GetUserById` returns a tracked entity and `Save()` flushes property changes. Do not call `AsNoTracking()` on update paths.
- `PUT /profile` is the only `[Authorize]` endpoint. All others (`register`, `login`, `refresh`, `validate`) are anonymous by design — `refresh` and `validate` receive the token in the body, not the `Authorization` header.
- `LoginUserDTO.Identifier` is intentionally a single field that accepts either email or `NombreUsuario`. If you need to distinguish them in the future, add a separate `loginBy` enum or a new endpoint rather than splitting the field.
- `AuthService` instantiates its own `PasswordHasher<Usuario>` (mirroring `MappersProfile`). If you want a single hasher instance, register `IPasswordHasher<Usuario>` in `Program.cs` and inject it — but keep the same hashing options, otherwise hashes written by `MappersProfile` will not verify.
- `register` and `login` both return the same `(accessToken, refreshToken, refreshTokenExpiration)` shape. They are typed as `RegisterResultDTO` and `LoginResultDTO` for semantic clarity; do not collapse them into one type without a deliberate refactor.
