# Ticketing System — Claude Code Guide

## What this project is

A Kanban-style ticket tracker built as a hackathon exercise. Users sign up, verify their email, then create teams, epics, and tickets that move through a fixed five-state workflow on a drag-and-drop board.

Full requirements: `Hackathon_Ticketing_System_Requirements_v3 1.docx`  
Full architecture: [`docs/architecture.md`](docs/architecture.md)

---

## Tech stack

| Concern | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API |
| Frontend | Angular 22 |
| Database | PostgreSQL 16 |
| ORM / migrations | Entity Framework Core 10 + Npgsql |
| CQRS bus | MediatR |
| Validation | FluentValidation |
| Password hashing | Argon2id (`Konscious.Security.Cryptography`) |
| Email | MailKit (SMTP) |
| Auth | JWT Bearer (`Authorization: Bearer <token>`) |
| Containerisation | Docker Compose |
| Drag-and-drop | `@angular/cdk/drag-drop` |

---

## Repository layout

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
│   ├── TicketingSystem.Infrastructure.Tests/
│   └── TicketingSystem.Api.IntegrationTests/
├── frontend/                            # Angular workspace — SKELETON ONLY, not yet implemented
├── deploy/
│   ├── compose.ps1                      # PowerShell up/down helper (Windows)
│   └── nginx/                           # Web tier: Dockerfile, reverse-proxy conf, SPA placeholder
├── env/                                 # Per-environment *.env.example templates (real env/*.env git-ignored)
├── docs/
│   └── architecture.md                  # Detailed architecture document
├── docker-compose.yml                   # Base topology (nginx → api → db)
├── docker-compose.{dev,test,prod}.yml   # Environment overrides
├── Makefile                             # make <env>-up / <env>-down / …
└── README.md
```

> **Note:** the Angular frontend is **not implemented yet** — `frontend/` contains only an empty directory skeleton. The architecture doc describes the intended structure, but no Angular code exists. nginx currently serves a placeholder page.

---

## Architecture in one paragraph

The backend follows **Clean Architecture**: the `Domain` layer contains pure C# entities and rules with no framework dependencies; the `Application` layer orchestrates use cases via MediatR commands and queries; the `Infrastructure` layer implements persistence (EF Core 10), email (MailKit), and auth (JWT); the `Api` layer is thin ASP.NET Core 10 controllers that dispatch to MediatR and handle HTTP concerns only. The Angular 22 frontend is split into a `core/` singleton layer, lazy-loaded `features/` modules, and a `shared/` component library. All three tiers run as separate Docker containers behind an nginx reverse proxy.

See [`docs/architecture.md`](docs/architecture.md) for layer diagrams, entity definitions, API contract, Docker topology, and key architectural decisions.

---

## Running the application

The stack (**nginx → api → postgres**) is packaged as a base `docker-compose.yml` plus one override per environment (`dev` / `test` / `prod`). Each environment runs under its own Compose project name and reads `env/<env>.env`.

```bash
cp env/dev.env.example env/dev.env     # defaults work as-is for local dev
make dev-up                            # or: ./deploy/compose.ps1 dev up   (Windows)
```

Raw equivalent (any platform, no make):

```bash
docker compose -f docker-compose.yml -f docker-compose.<env>.yml \
  -p ticketing-<env> --env-file env/<env>.env up --build
```

Edge ports: dev `:80`, test `:8081`, prod `:8080`. The API applies EF migrations on startup; Compose gates it on the DB health check. The API exposes `/health` (liveness) and `/health/ready` (DB readiness). No host-installed runtime is required beyond Docker (and optionally `make`).

---

## Configuration (environment variables)

Each environment's values live in `env/<env>.env` (copied from the committed `env/<env>.env.example`). **Never commit real `env/*.env` files** — they are git-ignored.

| Variable | Purpose |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | DB credentials; the API's `POSTGRES_CONNECTION_STRING` is assembled from these in compose |
| `JWT_SECRET_KEY` | HS256 signing key (≥ 256-bit random string) |
| `JWT_EXPIRATION_MINUTES` | Token lifetime (default 60) |
| `APP_BASE_URL` | Public URL used in verification email links |
| `ANGULAR_ORIGIN` | Allowed CORS origin for the SPA |
| `SMTP_HOST` | SMTP relay hostname (e.g. `relay1.dataart.com`; `maildev` in dev) |
| `SMTP_PORT` | SMTP port |
| `SMTP_USER` | SMTP username |
| `SMTP_PASSWORD` | SMTP password |

---

## Key domain rules

- **Epic–team coupling**: a ticket's epic must belong to the same team as the ticket; enforced in the domain entity and rejected by the backend with 400 if violated.
- **Delete guards**: a team cannot be deleted while it has tickets or epics; an epic cannot be deleted while tickets reference it. Both return HTTP 409 Conflict.
- **ModifiedAt precision**: saving unchanged ticket fields must not advance `ModifiedAt`. The entity compares values before updating the timestamp.
- **Verification tokens**: single-use, expire after 24 hours, stored as a SHA-256 hash. Issuing a new token invalidates previous unused ones.
- **Passwords**: always hashed with Argon2id before persistence; never logged or returned in any API response.

---

## Testing policy

Every code change — new feature or bug fix — must include the corresponding unit test changes in the same branch. Tests are not a separate follow-up step.

**Minimum unit test coverage: 80%.**

| Changed code | Where to add/update tests |
|---|---|
| Domain entities / value objects | `tests/TicketingSystem.Domain.Tests/` |
| Application command / query handlers | `tests/TicketingSystem.Application.Tests/` |
| API endpoints / middleware | `tests/TicketingSystem.Api.IntegrationTests/` |
| Angular components / services / pipes | Jest spec files alongside the changed file |

Do not consider a task done — and do not commit — if coverage for the changed code would fall below 80%.

---

## Branching strategy — trunk-based development

`main` is the trunk. All work flows through short-lived feature branches that merge back to `main` via pull request. There is no `develop`, `release`, or long-lived environment branch.

### Branch naming

```
feat/<short-description>      # new functionality
fix/<short-description>       # bug fixes
chore/<short-description>     # tooling, config, docs, dependency updates
```

### Rules for Claude Code

1. **Check branch and PR state before starting any work.**
2. **Open PR on the current branch** — keep working on the current branch; push additional commits to it when asked.
3. **Closed and merged PR** — pull `main` and cut a new `feat/*` / `fix/*` / `chore/*` branch before making changes.
4. **Never create a commit or open/update a PR automatically.** Stage changes and wait to be asked explicitly.
5. Feature branches are short-lived (hours to a couple of days). If a branch grows stale, flag it rather than silently accumulating commits.

### Typical flow

```
git switch main && git pull          # start from latest trunk
git switch -c feat/<topic>           # new short-lived branch
# ... make changes, run tests ...
# ask to commit → ask to push → ask to open PR
# PR reviewed and merged → branch deleted
```

---

## Explicit out-of-scope items

Scrum/sprints, SSO, fine-grained roles, file attachments, real-time updates, custom workflows, and production deployment hardening are all out of scope. Do not add them.
