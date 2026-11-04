# ATM App

A web-based ATM application for a single user who manages two bank accounts (Checking and
Savings). Built as a timeboxed (2–4 h) coding exercise focused on software design and architecture.

> **Status:** 🚧 Phase 4b (UI) implemented, awaiting review. The app runs end to end: the API has 61 backend tests, and the frontend has 23 unit and component tests. Dark mode is out of scope (cut line).
> See [.docs/TASKS.md](.docs/TASKS.md) for live progress.

## Features

| Feature | Status |
|---------|--------|
| View balances of both accounts | Done (UI and API) |
| Deposit funds | Done (UI and API) |
| Withdraw funds (no overdraft) | Done (UI and API) |
| Transfer between the two accounts (atomic) | Done (UI and API) |
| Transaction history per account | Done (UI and API) |
| Clear error messages for invalid operations | Done (UI and API) |

No authentication; this is a single-user app, per the requirements.

## Tech stack

| Layer    | Technology |
|----------|------------|
| Backend  | .NET 10, ASP.NET Core Web API, C# |
| Data     | EF Core + SQLite (file-based, created and seeded on startup) |
| Frontend | Vue 3.5 + Vite 8 + TypeScript 6.0, Tailwind CSS 4 |
| Tests    | Backend: xUnit unit tests (domain) and persistence tests on in-memory SQLite; `WebApplicationFactory` API tests come later. Frontend: Vitest + Vue Test Utils |

## Architecture (overview)

```
Vue 3 SPA  ──HTTP/JSON──►  Atm.Api             (controllers, validation, error handling)
                              │
                              ▼
                           Atm.Application     (use cases, DTOs)
                              │
                              ▼
                           Atm.Domain          (accounts, transactions, business rules)
                              ▲
                           Atm.Infrastructure  (EF Core + SQLite)
```

Business rules (no overdraft, valid amounts, one ledger entry per balance change) live in the
domain layer. Errors are returned as RFC 7807 `ProblemDetails`. Details and design decisions
are in [.docs/PLAN.md](.docs/PLAN.md).

## Getting started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22 LTS or newer

### Run

Backend (solution `backend/Atm.slnx`):

```bash
cd backend
dotnet build
dotnet test
dotnet run --project src/Atm.Api
```

Frontend:

```bash
cd frontend
npm install
npm run build
npm run dev        # http://localhost:5173 (proxies /api to the backend on port 5080)
npm run test       # unit tests (Vitest, jsdom)
```

The API runs on port 5080 and creates `backend/src/Atm.Api/atm.db` with two seeded accounts on first start.
Requests can be sent from [Atm.Api.http](backend/src/Atm.Api/Atm.Api.http) (VS Code REST Client). Open http://localhost:5173 to use the app.

## Project structure

```
atm-app/
├─ backend/
│  ├─ Atm.slnx
│  ├─ Directory.Build.props   # Shared settings: net10.0, nullable, warnings as errors
│  ├─ src/
│  │  ├─ Atm.Domain/          # Account, Transaction, amount rule, domain exceptions (references nothing)
│  │  ├─ Atm.Application/     # AccountService (use cases), DTOs, IAccountRepository (→ Domain)
│  │  ├─ Atm.Infrastructure/  # AtmDbContext, configurations, cents converter, repository, seeding (→ Application, Domain)
│  │  └─ Atm.Api/             # Controllers, request validation, ProblemDetails handler, DI wiring (→ Application, Infrastructure)
│  └─ tests/
│     ├─ Atm.Domain.Tests/    # xUnit + FluentAssertions 7
│     ├─ Atm.Infrastructure.Tests/  # Persistence on in-memory SQLite: cents, concurrency, seeding, transfers
│     └─ Atm.Api.Tests/       # WebApplicationFactory integration tests on in-memory SQLite (xUnit + FluentAssertions 7)
├─ frontend/                  # Vue 3 + Vite + TypeScript + Tailwind CSS 4: src/api, src/composables, src/types, src/utils
└─ .docs/
   ├─ REQUIREMENT.md   # Original exercise statement
   ├─ PLAN.md          # Scope, architecture, phases, decision log
   ├─ UX.md            # Lightweight UX and design spec
   └─ TASKS.md         # Progress checklist
```

## Development approach

The app is built with AI assistance (Claude Code) using a **human-in-the-loop** process:
the AI implements one phase at a time and stops at a review gate. I review, run, and approve
each step before the next one starts. The process is described in
[.docs/PLAN.md §3](.docs/PLAN.md), and the rules for the AI are in [CLAUDE.md](CLAUDE.md).

## Design decisions and tradeoffs
_To be summarized from the decision log ([.docs/PLAN.md §6](.docs/PLAN.md)) once decisions are
approved._

## How AI was used
_To be written by the author._

## What I'd do with more time
_To be completed at the end of the exercise._
