# Ticketing System — Architecture Document

## 1. Overview

This document describes the architecture for a Kanban-style ticketing system built as a three-tier SPA backed by PostgreSQL. The design applies **Clean Architecture** (Ports & Adapters / Onion Architecture) to keep business logic independent of framework, transport, and persistence concerns.

**Tech stack:**

| Tier | Technology |
|---|---|
| Frontend | Angular 22 |
| Backend | ASP.NET Core 10 Web API |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 10 |
| CQRS bus | MediatR |
| Validation | FluentValidation |
| Password hashing | Libsodium / Argon2id via `Konscious.Security.Cryptography` |
| Email | MailKit (SMTP) |
| Auth | JWT Bearer (Authorization header) |
| Containerisation | Docker Compose |

---

## 2. Clean Architecture Layers

Clean Architecture organises code in concentric layers. The dependency rule states that **source code dependencies must point inward only** — inner layers know nothing about outer layers.

```
╔══════════════════════════════════════════════╗
║              Presentation / API              ║  ← ASP.NET Core controllers, Angular
║  ┌────────────────────────────────────────┐  ║
║  │          Infrastructure                │  ║  ← EF Core, SMTP, JWT, migrations
║  │  ┌──────────────────────────────────┐  │  ║
║  │  │        Application               │  │  ║  ← CQRS commands/queries, DTOs,
║  │  │  ┌────────────────────────────┐  │  │  ║     validators, service interfaces
║  │  │  │         Domain             │  │  │  ║  ← Entities, value objects,
║  │  │  │   (no external deps)       │  │  │  ║     domain rules, repo interfaces
║  │  │  └────────────────────────────┘  │  │  ║
║  │  └──────────────────────────────────┘  │  ║
║  └────────────────────────────────────────┘  ║
╚══════════════════════════════════════════════╝
```

---

## 3. Backend Solution Structure

```
ticketing-system/
├── src/
│   ├── TicketingSystem.Domain/           # Inner core — no NuGet deps beyond BCL
│   ├── TicketingSystem.Application/      # Use cases — depends on Domain only
│   ├── TicketingSystem.Infrastructure/   # Adapters — depends on Application + Domain
│   └── TicketingSystem.Api/             # Entry point — depends on all layers (DI wiring)
├── tests/
│   ├── TicketingSystem.Domain.Tests/
│   ├── TicketingSystem.Application.Tests/
│   └── TicketingSystem.Api.IntegrationTests/
├── frontend/                             # Angular workspace
├── Directory.Build.props                 # Shared MSBuild properties for all projects
├── Directory.Packages.props              # Centralised NuGet package versions (CPM)
├── TicketingSystem.sln
├── docker-compose.yml
└── README.md
```

### 3.0 Centralised Build Configuration

All `.csproj` files are kept minimal — shared settings live in two repo-root MSBuild files that are automatically imported by the SDK.

**`Directory.Build.props`** — common MSBuild properties inherited by every project:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

**`Directory.Packages.props`** — [Central Package Management (CPM)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management): all NuGet versions declared once, referenced without versions in `.csproj` files:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Label="Framework">
    <PackageVersion Include="Microsoft.NET.Test.Sdk"         Version="17.12.0" />
  </ItemGroup>
  <ItemGroup Label="Application">
    <PackageVersion Include="MediatR"                        Version="12.4.1" />
    <PackageVersion Include="FluentValidation"               Version="11.11.0" />
    <PackageVersion Include="Konscious.Security.Cryptography.Argon2" Version="1.3.1" />
    <PackageVersion Include="MailKit"                        Version="4.9.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="Infrastructure">
    <PackageVersion Include="Microsoft.EntityFrameworkCore"  Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="Testing">
    <PackageVersion Include="xunit"                          Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio"      Version="2.8.2" />
    <PackageVersion Include="coverlet.collector"             Version="6.0.4" />
    <PackageVersion Include="Moq"                            Version="4.20.72" />
    <PackageVersion Include="Testcontainers.PostgreSql"      Version="4.4.0" />
  </ItemGroup>
</Project>
```

With CPM enabled, individual `.csproj` files use `<PackageReference Include="..." />` with **no `Version` attribute** — versions are resolved centrally. This eliminates version drift across projects and makes upgrades a single-line change.

### 3.1 Domain Layer (`TicketingSystem.Domain`)

Contains pure C# with no framework dependencies.

**Aggregates / Entities**

```
User
  ├── Id : Guid
  ├── Email : EmailAddress (value object — trimmed, lower-cased)
  ├── PasswordHash : string
  ├── IsEmailVerified : bool
  └── CreatedAt : DateTimeOffset (UTC)

EmailVerificationToken
  ├── Id : Guid
  ├── UserId : Guid
  ├── TokenHash : string        # store hash, not raw token
  ├── ExpiresAt : DateTimeOffset
  └── IsUsed : bool

Team
  ├── Id : Guid
  ├── Name : TeamName (value object — trimmed, unique case-insensitive)
  ├── CreatedAt : DateTimeOffset
  └── ModifiedAt : DateTimeOffset

Epic
  ├── Id : Guid
  ├── TeamId : Guid
  ├── Title : string
  ├── Description : string?
  ├── CreatedAt : DateTimeOffset
  └── ModifiedAt : DateTimeOffset

Ticket
  ├── Id : Guid
  ├── TeamId : Guid
  ├── EpicId : Guid?            # must belong to same team — enforced by domain rule
  ├── CreatedById : Guid
  ├── Type : TicketType         # enum: Bug | Feature | Fix
  ├── State : TicketState       # enum: New | ReadyForImplementation | InProgress |
  │                             #       ReadyForAcceptance | Done
  ├── Title : string
  ├── Body : string
  ├── CreatedAt : DateTimeOffset
  └── ModifiedAt : DateTimeOffset

Comment
  ├── Id : Guid
  ├── TicketId : Guid
  ├── AuthorId : Guid
  ├── Body : string
  └── CreatedAt : DateTimeOffset
```

**Domain rules (enforced inside entities, not in handlers)**

- `Ticket.ChangeEpic(epicTeamId)` throws `DomainException` if `epicTeamId != TeamId`.
- `Team` exposes `CanBeDeleted` computed from injected ticket/epic counts (passed in via domain service).
- `Ticket.SetState(newState)` updates `ModifiedAt` only if any field actually changed.
- `EmailVerificationToken.Use()` sets `IsUsed = true` and throws if expired or already used.

**Repository interfaces** (defined in Domain, implemented in Infrastructure)

```csharp
IUserRepository          // GetByEmail, GetById, Add, Update
ITeamRepository          // GetAll, GetById, Add, Update, Delete, HasTicketsOrEpics
IEpicRepository          // GetByTeam, GetById, Add, Update, Delete, HasTickets
ITicketRepository        // GetByTeam (with filters), GetById, Add, Update, Delete
ICommentRepository       // GetByTicket, Add
IVerificationTokenRepository  // GetByToken, Add, InvalidatePreviousForUser
```

**Value objects**

```csharp
EmailAddress   // normalises and validates format
TeamName       // trims, validates non-empty
TicketType     // Bug | Feature | Fix
TicketState    // New | ReadyForImplementation | InProgress | ReadyForAcceptance | Done
```

---

### 3.2 Application Layer (`TicketingSystem.Application`)

Orchestrates use cases via **CQRS with MediatR**. Depends on Domain only.

**Pattern: Command / Query separation**

```
Commands (write side — return void or Id)
  Auth
    SignUpCommand
    VerifyEmailCommand
    ResendVerificationCommand
    LoginCommand → returns JwtTokenDto

  Teams
    CreateTeamCommand → returns TeamDto
    RenameTeamCommand
    DeleteTeamCommand

  Epics
    CreateEpicCommand → returns EpicDto
    UpdateEpicCommand
    DeleteEpicCommand

  Tickets
    CreateTicketCommand → returns TicketDto
    UpdateTicketCommand
    ChangeTicketStateCommand    # used by Kanban drag-and-drop
    DeleteTicketCommand

  Comments
    AddCommentCommand → returns CommentDto

Queries (read side — return DTOs)
  GetTeamsQuery → IList<TeamDto>
  GetEpicsByTeamQuery → IList<EpicDto>
  GetTicketsByTeamQuery (filters: type, epicId, search) → IList<TicketSummaryDto>
  GetTicketDetailQuery → TicketDetailDto
  GetCommentsByTicketQuery → IList<CommentDto>
```

**Service interfaces** (implemented in Infrastructure)

```csharp
IEmailService          // SendVerificationEmailAsync(to, token)
IPasswordHasher        // Hash(password), Verify(hash, password)
IJwtService            // GenerateToken(userId, email) → string
ICurrentUserService    // UserId : Guid (populated from HttpContext in Api layer)
IDateTimeProvider      // UtcNow : DateTimeOffset (testable clock)
```

**Cross-cutting**

- FluentValidation validators registered per command/query (`AbstractValidator<T>`).
- `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behaviour runs validators before each handler.
- `TransactionBehavior<TRequest, TResponse>` wraps write commands in a DB transaction.

---

### 3.3 Infrastructure Layer (`TicketingSystem.Infrastructure`)

Implements all interfaces declared in Domain and Application. Depends on both inner layers.

**Persistence**

- EF Core 10 with Npgsql provider.
- One `AppDbContext` containing all `DbSet<T>` types.
- Fluent configuration via `IEntityTypeConfiguration<T>` classes (separate file per entity).
- Migrations managed with `dotnet ef migrations` and applied at startup via `context.Database.MigrateAsync()`.
- DB constraints: unique index on `users.email`, unique index on `teams.name_lower` (lower-case computed column), foreign key from `tickets.epic_id` → `epics.id` with `RESTRICT` delete rule, etc.

**Key EF configurations**

```
users           id, email (unique, lower-cased), password_hash, is_email_verified, created_at
verification_tokens   id, user_id (FK), token_hash, expires_at, is_used
teams           id, name, name_lower (generated), created_at, modified_at
epics           id, team_id (FK), title, description, created_at, modified_at
tickets         id, team_id (FK), epic_id (FK nullable), created_by_id (FK),
                type, state, title, body, created_at, modified_at
comments        id, ticket_id (FK cascade delete), author_id (FK), body, created_at
```

**Repository implementations** — thin wrappers around `AppDbContext` with LINQ queries. No business logic.

**Services**

- `ArgonPasswordHasher` — wraps `Konscious.Security.Cryptography.Argon2id`.
- `SmtpEmailService` — uses MailKit; SMTP host/port/credentials read from `IOptions<SmtpSettings>` (injected from environment variables, never hard-coded).
- `JwtService` — issues signed JWT with `sub`, `email`, `exp` claims; signing key from environment variable.
- `CurrentUserService` — reads `ClaimsPrincipal` from `IHttpContextAccessor`.
- `SystemDateTimeProvider` — returns `DateTimeOffset.UtcNow`.

---

### 3.4 API Layer (`TicketingSystem.Api`)

ASP.NET Core 10 minimal-hosting model. Depends on all layers for DI composition only.

**Controllers** (thin — no business logic, only dispatch to MediatR)

```
AuthController          POST /api/auth/signup
                        POST /api/auth/login
                        POST /api/auth/logout        (clears cookie / client-side)
                        GET  /api/auth/verify-email?token=…
                        POST /api/auth/resend-verification

TeamsController         GET    /api/teams
                        POST   /api/teams
                        PUT    /api/teams/{id}
                        DELETE /api/teams/{id}

EpicsController         GET    /api/teams/{teamId}/epics
                        POST   /api/teams/{teamId}/epics
                        PUT    /api/epics/{id}
                        DELETE /api/epics/{id}

TicketsController       GET    /api/teams/{teamId}/tickets   ?type=&epicId=&search=&state=
                        POST   /api/tickets
                        GET    /api/tickets/{id}
                        PUT    /api/tickets/{id}
                        PATCH  /api/tickets/{id}/state       (Kanban drag-and-drop)
                        DELETE /api/tickets/{id}

CommentsController      GET    /api/tickets/{ticketId}/comments
                        POST   /api/tickets/{ticketId}/comments
```

**Middleware pipeline (in order)**

1. HTTPS redirection
2. CORS (restrict to Angular origin in non-dev)
3. JWT Bearer authentication
4. `[Authorize]` enforced globally; public endpoints opt-out with `[AllowAnonymous]`
5. Global exception handler middleware → maps `DomainException` → 400, `NotFoundException` → 404, `ConflictException` → 409, unhandled → 500 with safe message
6. Request logging (Serilog)

**Authentication**

JWT Bearer tokens in the `Authorization: Bearer <token>` header. Tokens are **not** placed in URLs. Token expiry: 1 hour (configurable). Refresh is not required for mandatory scope.

**Configuration (environment variables only — never in source)**

```
POSTGRES_CONNECTION_STRING
JWT_SECRET_KEY
SMTP_HOST / SMTP_PORT / SMTP_USER / SMTP_PASSWORD
APP_BASE_URL          # used in verification email links
ASPNETCORE_ENVIRONMENT
```

---

## 4. Frontend Structure (Angular)

> **Reference wireframes:** see [`docs/ui/wireframes/`](ui/wireframes/README.md) for the
> low-fidelity screen mock-ups (extracted from §15 of the requirements spec) that
> map to the feature modules below. They illustrate the expected information
> hierarchy and primary user flows, but are not a binding visual design.

> **Implementation status:** the workspace is scaffolded with **Angular 22 + Angular
> Material**, standalone components, **zoneless** change detection, and signals.
> The **authentication flow is implemented** — `core/auth` (`AuthService`,
> `authGuard`, `TokenStorageService`), `core/http` + `core/error` functional
> interceptors, and the `features/auth` screens (login, signup, verify-email,
> resend-verification) per Wireframe 2. The JWT is held in `sessionStorage`. The
> verification email links to the SPA route `/verify-email?token=…`, which calls
> the API and renders the result. **Teams management** is also implemented —
> `core/teams/TeamsService` and the `features/teams` screen (MatTable list with
> ticket/epic counts + a create/edit **dialog** + delete confirmation) per Wireframe 4,
> against `GET/POST/PUT/DELETE /api/teams`. **Epics management** is implemented too —
> `core/epics/EpicsService` and the `features/epics` screen (team selector + epic
> list with ticket counts + a create/edit **dialog** + delete confirmation) per Wireframe 5,
> against `GET/POST /api/teams/{teamId}/epics` and `PUT/DELETE /api/epics/{id}`; an
> epic's team is fixed at creation. The **Board** is implemented as a **read-only**
> Kanban view — `core/tickets/TicketsService` and the `features/board` screen (team
> selector + the five fixed state columns of read-only ticket cards + client-side
> Type/Epic/title filters) per Wireframe 8, against `GET /api/teams/{teamId}/tickets`;
> ticket create/edit and drag-and-drop state changes are deferred. Guarded routes are
> hosted by a shared `shared/layout/MainLayoutComponent` (top nav Board/Teams/Epics +
> user menu); `/` redirects to `/board`.
> Unit tests use **Vitest** (Angular 22's built-in `ng test` runner, Node + jsdom).

```
frontend/src/app/
├── core/                     # Singleton services, guards, interceptors — imported once in AppModule
│   ├── auth/
│   │   ├── auth.service.ts        # login/signup/logout, JWT storage (memory or sessionStorage)
│   │   └── auth.guard.ts          # CanActivate — redirects to /login if unauthenticated
│   ├── http/
│   │   └── auth.interceptor.ts    # attaches Authorization header to every request
│   └── error/
│       └── error.interceptor.ts   # maps HTTP errors to user-visible messages
│
├── domain/                   # TypeScript interfaces mirroring API contracts
│   ├── team.model.ts
│   ├── epic.model.ts
│   ├── ticket.model.ts       # TicketType, TicketState enums + TicketSummary, TicketDetail
│   └── comment.model.ts
│
├── features/                 # Lazy-loaded feature modules
│   ├── auth/                 # /login, /signup, /verify-email, /resend-verification
│   ├── board/                # /board — read-only Kanban board, team selector, filters (drag-and-drop deferred)
│   ├── ticket-detail/        # /tickets/:id — view, edit, comments
│   ├── teams/                # /teams — team list and CRUD
│   └── epics/                # /epics — epic list and CRUD per team
│
└── shared/                   # Reusable UI components, pipes, directives
    ├── components/
    │   ├── confirm-dialog/
    │   ├── loading-spinner/
    │   └── error-banner/
    └── pipes/
        └── ticket-state-label.pipe.ts   # 'ready_for_implementation' → 'Ready for Implementation'
```

**State management**

- Local component state + Angular services with `BehaviorSubject` for board data.
- No full NgRx store required at this scope; a lightweight signal-based approach (Angular 17+ signals) is preferred.
- The Kanban board optimistically moves the card on drag-drop, then calls `PATCH /api/tickets/{id}/state`. On error it reverts the card to its original column and shows an error banner.

**Drag-and-drop**

Use `@angular/cdk/drag-drop` (`CdkDragDrop`, `cdkDropList`, connected lists). No third-party DnD library needed.

**Routing (summary)**

```
/                       → redirect to /board
/login                  → AuthModule (public)
/signup                 → AuthModule (public)
/verify-email           → AuthModule (public)
/resend-verification    → AuthModule (public)
/board                  → BoardModule (guarded)
/tickets/:id            → TicketDetailModule (guarded)
/teams                  → TeamsModule (guarded)
/epics                  → EpicsModule (guarded)
```

---

## 5. Domain Model Diagram

```
User ──────────────────────────────────────────────────────────────────────┐
 │                                                                         │
 │ creates                                                                 │
 ▼                                                                     authored
Ticket ─── belongs to ──► Team ◄── contains ── Epic                  Comment
 │                                    ▲                                    │
 └── optionally references ───────────┘           references ◄────────────┘
       (same team enforced)
```

**Key integrity rules**

| Rule | Enforced by |
|---|---|
| Epic must belong to same team as Ticket | Domain entity + DB check in Application handler + DB FK |
| Team delete blocked if has tickets or epics | Domain service + HTTP 409 response |
| Epic delete blocked if referenced by tickets | Domain service + HTTP 409 response |
| Email unique, case-insensitive | DB unique index on lower-cased column + Application validation |
| Password never stored plain | Infrastructure `ArgonPasswordHasher` always called before persistence |
| Verification token single-use, expires 24 h | Domain entity `Use()` method + token expiry check |

---

## 6. API Contract Summary

All timestamps: ISO-8601 UTC (e.g., `"2026-06-29T14:00:00Z"`).  
All IDs: UUID v4 strings.  
Auth: `Authorization: Bearer <jwt>` header on all protected endpoints.

### HTTP Status Code Conventions

| Scenario | Status |
|---|---|
| Success (read) | 200 OK |
| Success (created) | 201 Created + `Location` header |
| Validation error | 400 Bad Request + `{ errors: {...} }` |
| Unauthenticated | 401 Unauthorized |
| Not found | 404 Not Found |
| Delete blocked (references exist) | 409 Conflict |
| Server error | 500 Internal Server Error (safe message only) |

---

## 7. Docker Compose Topology

```
┌─────────────────────────────────────────────────────┐
│  docker-compose.yml                                  │
│                                                      │
│  ┌──────────┐     ┌──────────────┐     ┌──────────┐ │
│  │  nginx   │────►│  api         │────►│  db      │ │
│  │  :80     │     │ (ASP.NET 8)  │     │(Postgres)│ │
│  │  serves  │     │  :8080       │     │  :5432   │ │
│  │  Angular │     │              │     │          │ │
│  │  dist/   │     │              │     │          │ │
│  └──────────┘     └──────────────┘     └──────────┘ │
└─────────────────────────────────────────────────────┘
```

- **nginx** serves the compiled Angular SPA (`ng build --configuration production`) and proxies `/api/**` to the API container.
- **api** runs `dotnet TicketingSystem.Api.dll`; applies EF migrations on startup before serving requests.
- **db** is the official `postgres:16-alpine` image; data persisted in a named Docker volume.
- All secrets injected via `.env` file (git-ignored); `.env.example` committed with placeholder values.

---

## 8. Security Considerations

- Passwords hashed with **Argon2id** (memory-hard; resistant to GPU/ASIC attacks).
- Verification tokens stored as a **SHA-256 hash** in the DB; only the raw token travels in the email link.
- JWT signing key is ≥ 256-bit random value set via environment variable.
- CORS restricted to the known Angular origin; credentials flag enabled only if cookie auth path is chosen.
- All endpoints except auth and static assets require a valid JWT.
- SMTP credentials never appear in source code or committed configuration files.
- Input validated server-side on every write operation; client-side validation is a UX supplement only.
- EF Core parameterised queries prevent SQL injection by default.

---

## 9. Testing Strategy

| Layer | Tool | What is covered |
|---|---|---|
| Domain | xUnit | Entity invariants, value object rules, domain exceptions |
| Application | xUnit + Moq | Command/query handlers with mocked repositories |
| API integration | xUnit + Testcontainers (Postgres) | Full HTTP round-trips, auth flows, 409/404 scenarios |
| Frontend unit | Vitest (Angular `ng test`) | Services, pipes, guard logic, component logic |
| Frontend E2E | Playwright | At least one happy-path flow (signup → verify → create ticket → drag to Done) |

---

## 10. Key Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Architecture style | Clean Architecture | Business logic isolated from EF Core, ASP.NET, Angular; independently testable |
| CQRS pattern | MediatR | Separates read/write paths, enables pipeline behaviours (validation, transactions) |
| Auth mechanism | JWT Bearer | Stateless; easy CORS setup; requirement-compliant (not in URLs) |
| ID type | UUID (Guid) | No sequential enumeration risk; works across distributed scenarios |
| Kanban state change path | Dedicated `PATCH /state` endpoint | Avoids full-object PUT for a single field; enables optimistic UI revert on failure |
| Epic-Team coupling | Enforced in domain entity + DB FK | Two enforcement points prevent data corruption even under concurrent writes |
| Frontend DnD | `@angular/cdk/drag-drop` | Zero additional dependency; maintained by Angular team |
| Clock abstraction | `IDateTimeProvider` | Allows deterministic time in tests without mocking static `DateTime.UtcNow` |
