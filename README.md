# ATM App

A web-based ATM application for a single user who manages two bank accounts (Checking and
Savings). Built as a timeboxed (2–4 h) coding exercise focused on software design and architecture.

> **Status:** 🚧 Phase 0 (scaffold) done. Solution and frontend build; no domain logic yet.
> See [.docs/TASKS.md](.docs/TASKS.md) for live progress.

## Features

| Feature | Status |
|---------|--------|
| View balances of both accounts | Planned |
| Deposit funds | Planned |
| Withdraw funds (no overdraft) | Planned |
| Transfer between the two accounts (atomic) | Planned |
| Transaction history per account | Planned |
| Clear error messages for invalid operations | Planned |

No authentication; this is a single-user app, per the requirements.

## Tech stack

| Layer    | Technology |
|----------|------------|
| Backend  | .NET 10, ASP.NET Core Web API, C# |
| Data     | EF Core + SQLite (file-based, created and seeded on startup) |
| Frontend | Vue 3.5 + Vite 8 + TypeScript 6.0, Tailwind CSS 4 |
| Tests    | Backend: xUnit unit tests + `WebApplicationFactory` integration tests. Frontend: Vitest + Vue Test Utils |

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
npm run dev        # http://localhost:5173
```

The API endpoints and the UI are not implemented yet, so the app is not usable end to end
until later phases land.

## Project structure

```
atm-app/
├─ backend/
│  ├─ Atm.slnx
│  ├─ Directory.Build.props   # Shared settings: net10.0, nullable, warnings as errors
│  ├─ src/
│  │  ├─ Atm.Domain/          # Entities and rules (references nothing)
│  │  ├─ Atm.Application/     # Use cases and DTOs (→ Domain)
│  │  ├─ Atm.Infrastructure/  # EF Core + SQLite (→ Application, Domain)
│  │  └─ Atm.Api/             # Controllers and DI wiring (→ Application, Infrastructure)
│  └─ tests/
│     ├─ Atm.Domain.Tests/    # xUnit + FluentAssertions 7
│     └─ Atm.Api.Tests/       # xUnit + FluentAssertions 7 (integration tests later)
├─ frontend/                  # Vue 3 + Vite + TypeScript + Tailwind CSS 4
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
