# Ticketing System

A Kanban-style ticket tracker. Users sign up, verify their email, create teams and epics, then manage tickets across a five-state workflow on a drag-and-drop board.

## Tech stack

| Concern | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API |
| Frontend | Angular 22 |
| Database | PostgreSQL 16 |
| ORM / migrations | Entity Framework Core 10 + Npgsql |
| CQRS | MediatR |
| Validation | FluentValidation |
| Password hashing | Argon2id (`Konscious.Security.Cryptography`) |
| Email | MailKit (SMTP) |
| Auth | JWT Bearer (`Authorization: Bearer <token>`) |
| Containerisation | Docker Compose |
| Drag-and-drop | `@angular/cdk/drag-drop` |

## Prerequisites

- Docker and Docker Compose
- *(optional)* GNU `make` — for the `make <env>-up/down` convenience targets. On Windows you can instead use `deploy/compose.ps1` or run `docker compose` directly.

No host-installed runtime (.NET SDK, Node.js) is required to run the stack.

## Getting started (development)

```bash
cp env/dev.env.example env/dev.env      # defaults work as-is for local dev
make dev-up                             # or, on Windows: ./deploy/compose.ps1 dev up
```

…or the equivalent raw command on any platform:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml \
  -p ticketing-dev --env-file env/dev.env up --build
```

Once the containers are healthy:

- App / API edge: <http://localhost> (nginx serves a placeholder until the Angular app is built; API is proxied under `/api/`)
- Health: <http://localhost/health> and `…/health/ready`
- MailDev inbox (captures verification emails in dev): <http://localhost:1080>

## Deployment (dev / test / prod)

The stack is **nginx → api → postgres** (see [`docs/architecture.md`](docs/architecture.md) §7), packaged as a base `docker-compose.yml` plus one override per environment. Each environment runs under its own Compose project name so they can coexist on one host.

| Environment | `ASPNETCORE_ENVIRONMENT` | Edge port | DB port | Extras |
|---|---|---|---|---|
| dev  | Development | 80   | 5432 | MailDev (1080) |
| test | Test        | 8081 | 5433 | — |
| prod | Production  | 8080 | not published | restart policies |

```bash
# Make targets (Linux/macOS/CI, or Windows with GNU make)
make dev-up        # build + start the dev stack (detached)
make dev-down      # stop it
make dev-down-v    # stop it and drop the database volume
make dev-logs      # follow logs
# …same for test-* and prod-*; or generic: make up ENV=prod / make down ENV=test
make help          # list all targets

# PowerShell helper (Windows)
./deploy/compose.ps1 <dev|test|prod> up      # add -Detach to run in the background
./deploy/compose.ps1 <dev|test|prod> down

# Raw equivalent
docker compose -f docker-compose.yml -f docker-compose.<env>.yml \
  -p ticketing-<env> --env-file env/<env>.env up --build
```

The API applies EF Core migrations on startup; Compose waits for the database's health check before starting it, so the first boot provisions the schema automatically.

> **Note:** the Angular frontend is not implemented yet — nginx serves a placeholder page. When the frontend lands, the web image's build stage (commented in `deploy/nginx/Dockerfile`) compiles and serves the Angular bundle with no compose changes.

## Configuration

Each environment reads its values from `env/<env>.env` (copied from the committed `env/<env>.env.example`). **Never commit the real `env/*.env` files** — they are git-ignored.

| Variable | Purpose |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Database credentials; the API's connection string is assembled from these |
| `JWT_SECRET_KEY` | HS256 signing key — minimum 32 random bytes |
| `JWT_EXPIRATION_MINUTES` | Token lifetime (default 60) |
| `APP_BASE_URL` | Public URL used in verification email links |
| `ANGULAR_ORIGIN` | Allowed CORS origin for the SPA |
| `SMTP_HOST` / `SMTP_PORT` / `SMTP_USER` / `SMTP_PASSWORD` | SMTP relay settings |

## Project structure

```
ticketing-system/
├── src/
│   ├── TicketingSystem.Domain/          # Entities, value objects, repo interfaces
│   ├── TicketingSystem.Application/     # CQRS commands/queries, DTOs, validators
│   ├── TicketingSystem.Infrastructure/  # EF Core, SMTP, JWT, repositories
│   └── TicketingSystem.Api/             # ASP.NET Core controllers, middleware, DI root
│       └── Dockerfile                   # Multi-stage API image (build context = repo root)
├── tests/
│   ├── TicketingSystem.Domain.Tests/
│   ├── TicketingSystem.Application.Tests/
│   ├── TicketingSystem.Infrastructure.Tests/
│   └── TicketingSystem.Api.IntegrationTests/
├── deploy/
│   ├── compose.ps1                      # PowerShell up/down helper (Windows)
│   └── nginx/                           # Web tier: Dockerfile, reverse-proxy conf, SPA placeholder
├── env/                                 # Per-environment *.env.example templates
├── docker-compose.yml                   # Base topology (nginx → api → db)
├── docker-compose.{dev,test,prod}.yml   # Environment overrides
├── Makefile                             # make <env>-up / <env>-down / …
├── frontend/                            # Angular workspace (skeleton only — not implemented yet)
└── docs/
    └── architecture.md                  # Detailed architecture and design decisions
```

The backend follows Clean Architecture: `Domain` → `Application` → `Infrastructure` → `Api`, with dependencies pointing inward only. See [`docs/architecture.md`](docs/architecture.md) for layer diagrams, entity definitions, and the full API contract.

## Running tests

```bash
dotnet test
```

Minimum coverage target is 80 % per changed file.

## Key domain rules

- A ticket's epic must belong to the same team as the ticket.
- A team cannot be deleted while it has tickets or epics (HTTP 409).
- An epic cannot be deleted while tickets reference it (HTTP 409).
- Verification tokens are single-use, expire after 24 hours, and are stored as SHA-256 hashes.
- Passwords are always hashed with Argon2id before persistence and are never returned by the API.
