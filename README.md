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

No host-installed runtime (.NET SDK, Node.js) is required.

## Getting started

```bash
cp .env.example .env
# Edit .env and fill in SMTP credentials and a JWT secret
docker compose up --build
```

The app is available at `http://localhost` once all containers are healthy.

## Configuration

All secrets are provided via the `.env` file. **Never commit real values.**

| Variable | Purpose |
|---|---|
| `POSTGRES_CONNECTION_STRING` | PostgreSQL connection string |
| `JWT_SECRET_KEY` | HS256 signing key — minimum 32 random bytes |
| `SMTP_HOST` | SMTP relay hostname |
| `SMTP_PORT` | SMTP port |
| `SMTP_USER` | SMTP username |
| `SMTP_PASSWORD` | SMTP password |
| `APP_BASE_URL` | Public URL used in verification email links |

## Project structure

```
ticketing-system/
├── src/
│   ├── TicketingSystem.Domain/          # Entities, value objects, repo interfaces
│   ├── TicketingSystem.Application/     # CQRS commands/queries, DTOs, validators
│   ├── TicketingSystem.Infrastructure/  # EF Core, SMTP, JWT, repositories
│   └── TicketingSystem.Api/             # ASP.NET Core controllers, middleware, DI root
├── tests/
│   ├── TicketingSystem.Domain.Tests/
│   ├── TicketingSystem.Application.Tests/
│   └── TicketingSystem.Api.IntegrationTests/
├── frontend/                            # Angular workspace
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
