# ATM App

A web-based ATM application for a single user who manages two bank accounts (Checking and
Savings). Built as a timeboxed (2–4 h) coding exercise focused on software design and architecture.

> **Status:** Done. All phases and gates are complete, and the app runs end to end:
> 62 backend tests and 23 frontend tests pass. Dark mode is not included (cut line).
> See [.docs/TASKS.md](.docs/TASKS.md) for the full checklist.

## Features

| Feature | Status |
|---------|--------|
| View balances of both accounts | Done |
| Deposit funds | Done |
| Withdraw funds (no overdraft) | Done |
| Transfer between the two accounts (atomic) | Done |
| Transaction history per account (latest 20) | Done |
| Clear error messages for invalid operations | Done |

No authentication; this is a single-user app, per the requirements.

## Tech stack

| Layer    | Technology |
|----------|------------|
| Backend  | .NET 10, ASP.NET Core Web API, C# 13 |
| Data     | EF Core + SQLite (file-based, created and seeded on startup) |
| Frontend | Vue 3.5 + Vite 8 + TypeScript 6.0, Tailwind CSS 4 |
| Tests    | Backend: xUnit + FluentAssertions 7. Domain unit tests, persistence tests on in-memory SQLite, and API integration tests with `WebApplicationFactory`. Frontend: Vitest + Vue Test Utils (jsdom) |

## Architecture (overview)

```
Vue 3 SPA  ──HTTP/JSON (/api)──►  Atm.Api             (controllers, validation, error handling)
                                     │
                                     ▼
                                  Atm.Application     (AccountService use cases, DTOs, repository port)
                                     │
                                     ▼
                                  Atm.Domain          (Account, Transaction, business rules)
                                     ▲
                                  Atm.Infrastructure  (EF Core + SQLite, implements the repository port)
```

The dependency rule is enforced by project references: Domain references nothing, Application
references Domain, Infrastructure references Application and Domain, and Api references Application
and Infrastructure.

The frontend follows the same idea in three layers: components (presentational) → composable
(`useAccounts`, state and actions) → API client (`src/api`, the only code that calls `fetch`).

**Where the rules live**
- The domain enforces the money rules: amounts greater than zero, at most 2 decimal places and
  1,000,000 per operation, no overdraft, no transfer to the same account. Each balance change
  writes exactly one ledger entry, and the account's `Version` changes with it.
- A transfer changes both accounts in memory and saves once, so the database writes both legs
  in one transaction.
- Errors are RFC 7807 `ProblemDetails`. Domain rule violations return 422, a missing account 404,
  a concurrent write 409, and a malformed request 400. Anything unexpected returns a generic 500,
  and the details are only logged.

Full details and every decision are in [.docs/PLAN.md](.docs/PLAN.md).

## Project structure

```
atm-app/
├─ README.md
├─ .docs/                       # REQUIREMENT, PLAN (decision log), UX spec, TASKS checklist
├─ backend/
│  ├─ Atm.slnx
│  ├─ Directory.Build.props     # Shared settings: net10.0, nullable, warnings as errors
│  ├─ src/
│  │  ├─ Atm.Domain/            # Account, Transaction, AmountRules, domain exceptions
│  │  ├─ Atm.Application/       # AccountService, DTOs, IAccountRepository
│  │  ├─ Atm.Infrastructure/    # AtmDbContext, EF configurations, cents converter, seeding
│  │  └─ Atm.Api/               # Controllers, request records, GlobalExceptionHandler, Program.cs
│  └─ tests/
│     ├─ Atm.Domain.Tests/          # Unit tests for the business rules
│     ├─ Atm.Infrastructure.Tests/  # Persistence tests on in-memory SQLite
│     └─ Atm.Api.Tests/             # Integration tests with WebApplicationFactory
└─ frontend/
   └─ src/
      ├─ api/                   # Typed HTTP client (the only code that calls fetch)
      ├─ composables/           # useAccounts: state and actions
      ├─ components/            # AccountCard, OperationPanel, TransactionHistory, StatusMessage, ServerBanner
      ├─ types/                 # TypeScript types mirroring the API DTOs
      └─ utils/                 # Amount validation and Intl formatting
```

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
dotnet run --project src/Atm.Api     # http://localhost:5080
```

Frontend (in a second terminal):

```bash
cd frontend
npm install
npm run test
npm run build
npm run dev                          # http://localhost:5173, proxies /api to port 5080
```

Then open **http://localhost:5173**. The API creates `backend/src/Atm.Api/atm.db` with two seeded
accounts (Checking $1,000 and Savings $5,000) on first start. To delete the data, stop the API and
remove that file.

Requests can also be sent one by one from
[Atm.Api.http](backend/src/Atm.Api/Atm.Api.http) using the VS Code REST Client extension.

## Design decisions and tradeoffs

The full decision log is in [PLAN.md §6](.docs/PLAN.md). The main choices:

- **Layers and rules.** Business rules live on `Account`, not in a service, so they can be unit
  tested without a database (#2). The layers are separate projects, so the dependency rule is
  checked by the compiler (#1).
- **Money as cents.** Stored as integer cents through a value converter, because SQLite has no
  decimal type and REAL loses precision (#5). Amounts with a fraction of a cent are rejected, not
  rounded (#19, #28).
- **Concurrency.** An optimistic `Version` token, checked on every update, so a stale write fails
  with 409 instead of silently overwriting a balance (#7, #24).
- **Atomic transfers.** Both legs are changed in memory and saved with one `SaveChangesAsync`, so
  the database writes them in one transaction (#21).
- **Ledger.** Every balance change is a ledger entry, and transfer legs share a `TransferId` (#8).
- **Errors.** Domain exceptions plus one global handler, instead of a `Result<T>` type (#6). One
  place to change the error contract (#35).
- **Validation in two places.** The API checks request shape (400) and the amount range, and the
  domain checks every rule (422). The domain is the authority (#34).
- **No heavy frameworks.** No MediatR, AutoMapper or generic repositories (#9). The frontend uses a
  composable rather than Pinia (#10). The API is passed into the composable, so tests can use fakes
  without mocking `fetch` (#38).
- **Frontend validation.** The browser validates the amount for instant feedback, but the backend
  stays the authority (#39).
- **Swap instead of a destination picker.** With two accounts, the transfer form has a swap button,
  so a same-account transfer can't be chosen (#41, UX §3).
- **Trade-off on plain HTTP.** The API runs on plain HTTP on localhost, with no HTTPS redirect (#33).
- **Seeding and schema.** `EnsureCreated` with no migrations, since the schema is fixed (#4). The
  database is seeded at startup (#30, #37).

## How AI was used

I built this with Claude Code, using a human-in-the-loop process. The work was split into gated phases, and the AI stopped at the end of each one. Before the next phase started, I reviewed the code, ran the tests and the app, and approved or changed each step. The decisions are in [PLAN.md §6](.docs/PLAN.md), and the record of my review and changes is in [PLAN.md §7](.docs/PLAN.md).

The planning documents built on each other:
- **Requirements** ([`.docs/REQUIREMENT.md`](.docs/REQUIREMENT.md)): the exercise statement, which sets the scope.
- **Plan** ([`.docs/PLAN.md`](.docs/PLAN.md)): the architecture, phases, gates and decisions.
- **Tasks** ([`.docs/TASKS.md`](.docs/TASKS.md)): the checklist of implementation steps and the only progress tracker.

Higher-level work, such as the requirements and the plan, was done with Claude Opus 5.5. The task breakdown and most of the implementation were done with Claude Haiku 5.5.

The AI made mistakes that the tests or my review caught. For example, EF Core treated new ledger rows as existing rows until the key mapping was set explicitly, and ASP.NET failed at runtime on validation attributes placed on record properties, which an error handler had hidden behind a 500. I can explain the design choices in the code, including the ones the AI proposed.

## What I'd do with more time

- **Schema and data.** Replace `EnsureCreated` with migrations, and move the demo seeding behind a
  setting (#4).
- **History paging.** Add paging in the history view, which shows the latest 20 entries (the API
  already supports paging).
- **Confirmation and receipts.** Add an optional confirm step for larger amounts, and show a receipt.
- **Money on the wire.** Send amounts as strings rather than JSON numbers, which removes any
  question of floating-point conversion between the browser and the server.
- **Security and identity.** Authentication and card/PIN flows, which are out of scope now.
- **End-to-end tests.** Add Playwright tests for the full user flows (decision #14 lists this as a
  next step).
- **Accessibility and polish.** Dark mode, and a check of contrast and keyboard order with an
  accessibility tool.
- **Generated client.** Generate the frontend API types from the OpenAPI document, so the DTO types
  can't drift from the backend.
- **Idempotency.** Idempotency keys for retried requests (out of scope in PLAN §1).
- **Counterparty in the API.** The history currently derives transfer labels from the account list,
  which works only with two accounts. The API could return the counterparty name directly.
