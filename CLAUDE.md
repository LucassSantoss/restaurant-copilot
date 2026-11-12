# RestaurantCopilot

ASP.NET Core (.NET 10) API where restaurant owners ask natural-language questions about their business; an AI agent (Google Gemini) answers using function calling, RAG (PostgreSQL + pgvector) and guarded text-to-SQL. This is a portfolio project: code quality, structure and documentation matter as much as features.

## Commands

```bash
docker compose up -d postgres redis              # local dependencies
dotnet run --project backend/src/RestaurantCopilot.Api   # http://localhost:5080 (/scalar, /health)
cd backend && dotnet test                         # needs Docker (Testcontainers)
cd backend && dotnet format --verify-no-changes   # lint, same as CI
docker compose up -d --build                      # full stack, API on :8080
```

## Architecture

Clean Architecture in `backend/src`, dependencies point inward: `Api → Infrastructure → Application → Domain`.

- `Domain`: entities, value objects, pure rules. No external dependencies.
- `Application`: use cases (own lightweight CQRS handlers, no MediatR), DTOs, ports (`ILlmClient`, `IEmbeddingService`, `IVectorStore`, `IAgentTool`, `ICacheService`, `ISqlQueryGuard`). Never references EF Core, Npgsql, Redis or ASP.NET Core.
- `Infrastructure`: implementations of the ports (EF Core/Postgres, pgvector, Redis, Gemini).
- `Api`: endpoints, DI composition, middlewares. No business logic.

Architecture tests in `backend/tests/RestaurantCopilot.Api.Tests/Architecture` enforce these rules. The app models a single restaurant (no multi-tenancy, no auth in the MVP).

## Conventions

- Code, identifiers and commit messages in English; docs (README, ADRs) in Portuguese, README has an English summary.
- Conventional Commits, imperative lowercase: `feat(agent): add sales lookup tool`. Small, focused commits pushed straight to `main`.
- Nullable on, warnings as errors in production projects, `.editorconfig` at the root, package versions only in `backend/Directory.Packages.props`.
- Business errors return `Result<T>`; exceptions are not used for control flow.
- Every use case has a unit test; repositories and endpoints have integration tests (Testcontainers, real Postgres/Redis).
- Relevant technical decisions get an ADR in `docs/adr/` (update the index there). No dead code, leftover TODOs or committed secrets.
- Gemini model names and the API key come from configuration (`Gemini__*`, user-secrets, `.env`), never hard-coded.
- Seed data is fictitious only.
