# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Context

This is a **timeboxed coding exercise (2–4 hours)**: a web-based ATM app for a single user with
two bank accounts. The author must be able to **explain and defend every line**, so clarity beats
cleverness. The grading criteria are architecture, separation of concerns, error handling,
code quality and readability, and technical tradeoffs.

The original exercise statement is in [.docs/REQUIREMENT.md](.docs/REQUIREMENT.md) and is the
source of truth for scope. The implementation plan, scope and decision log are in
[.docs/PLAN.md](.docs/PLAN.md). Progress is tracked **only** in [.docs/TASKS.md](.docs/TASKS.md).
Read all three before starting a task. Tick `[C]` tasks in TASKS.md as they're completed and
verified, and add new decisions to the PLAN.md decision log.

## Stack

| Layer     | Choice                                                        |
|-----------|---------------------------------------------------------------|
| Backend   | .NET 10, ASP.NET Core Web API (controllers), C# 13            |
| Data      | EF Core + SQLite (file `atm.db`, auto-created and seeded)     |
| Frontend  | Vue 3.5 + Vite 8 + TypeScript 6.0 (template pin, not 7.x; see decision #13), Composition API (`<script setup>`), Tailwind CSS 4 via `@tailwindcss/vite` (decision #16). Exact versions locked by `package-lock.json`. |
| Tests     | xUnit + FluentAssertions 7.x (backend, see decision #12), `WebApplicationFactory` for API tests. Frontend: Vitest 5 + `@vue/test-utils` + jsdom (decision #14). |

## Repository layout

```
atm-app/
├─ README.md                  # Overview, how to run, status; updated every phase
├─ backend/
│  ├─ Atm.slnx
│  ├─ src/
│  │  ├─ Atm.Domain/          # Entities, value rules, domain exceptions. NO dependencies.
│  │  ├─ Atm.Application/     # Use cases (AccountService), DTOs, repository interfaces.
│  │  ├─ Atm.Infrastructure/  # EF Core DbContext, configurations, repositories, seeding.
│  │  └─ Atm.Api/             # Controllers, request validation, exception → ProblemDetails, DI wiring.
│  └─ tests/
│     ├─ Atm.Domain.Tests/
│     ├─ Atm.Infrastructure.Tests/  # Persistence tests on in-memory SQLite (decision #29).
│     └─ Atm.Api.Tests/       # Integration tests via WebApplicationFactory.
├─ .docs/
│  ├─ REQUIREMENT.md          # Original exercise statement (do not edit)
│  ├─ PLAN.md                 # Plan, gates, decision log, HITL log
│  ├─ UX.md                   # Lightweight UX spec = acceptance criteria for the UI phase
│  └─ TASKS.md                # Checklist: the single progress tracker
└─ frontend/                  # Vue 3 + Vite app
   └─ src/
      ├─ api/                 # Typed HTTP client (only place that calls fetch)
      ├─ composables/         # State + actions (useAccounts)
      ├─ components/          # Presentational components
      ├─ types/               # TS types mirroring API DTOs
      └─ utils/               # Amount validation and Intl formatting
```

## Commands

```bash
# Backend
cd backend
dotnet build
dotnet test
dotnet run --project src/Atm.Api            # http://localhost:5080, Swagger/OpenAPI at /openapi/v1.json

# Frontend (requires Node 22 LTS+)
cd frontend
npm install
npm run dev                                  # http://localhost:5173, proxies /api -> backend
npm run build
npm run test                                 # Vitest unit + component tests
```

## Architecture rules (keep them)

- **Dependency direction:** Api → Application → Domain, Infrastructure → Application/Domain.
  Domain references nothing. Application does not reference EF Core.
- **Business rules live in the Domain.** `Account.Deposit/Withdraw` enforce invariants
  (positive amount, max 2 decimals, no overdraft) and record their own `Transaction` entries.
  Controllers stay thin: validate the request shape, call the service, map to a response.
- **Transfers are atomic:** both account mutations and both ledger entries are persisted in a
  single `SaveChangesAsync` call (one DB transaction). Transfer legs share a `TransferId`.
- **Money:** `decimal` in C#, amounts with more than 2 decimals are rejected, not rounded (decisions #19, #28). Stored as integer cents
  in SQLite through an EF value converter, because SQLite has no native decimal type.
  Frontend sends and receives numbers but formats with `Intl.NumberFormat`.
- **Concurrency:** `Account` has a `Version` concurrency token. A concurrent update returns
  **409 Conflict**.
- **Error handling:** domain failures throw typed exceptions (`InsufficientFundsException`,
  `InvalidAmountException`, `AccountNotFoundException`, `SameAccountTransferException`).
  A single global `IExceptionHandler` maps them to RFC 7807 `ProblemDetails`
  (400 / 404 / 409 / 422). Unexpected errors return 500 without leaking internals and are logged.
- **No over-engineering:** no MediatR, CQRS, generic repositories, AutoMapper or auth.
  Add an abstraction only when it has a concrete use in this codebase.

## Coding conventions

- C#: file-scoped namespaces, nullable enabled, `TreatWarningsAsErrors` on, records for DTOs,
  `async` all the way with `CancellationToken` passed through. Private setters on entities.
- Name things after the domain (Deposit, Withdraw, Transfer, Ledger/Transaction), not after
  generic CRUD.
- Vue: `<script setup lang="ts">`, props/emits typed, no `any`. Components don't call `fetch`
  directly; they go through `src/api`.
- UI follows [.docs/UX.md](.docs/UX.md): Tailwind utilities using the `@theme` tokens (no arbitrary
  values like `bg-[#…]`, no component library, `@apply` only sparingly; extract a small component
  when a class list repeats), `Intl` for money and dates, labelled inputs, `aria-live` for status messages, no `alert()`, and never show raw
  JSON or stack traces to the user.
- Write comments only for *why*, not *what*.

## Working agreement for Claude: human-in-the-loop

This project follows the gate process in [.docs/PLAN.md §3](.docs/PLAN.md). The human owns decisions;
Claude proposes and implements.

- **Work only on the current phase.** Never start the next phase, or "get ahead" on it,
  without an explicit `approved` from the human.
- **At the end of every phase, stop and present the gate packet** (PLAN.md §3): a summary,
  build/test results, new decisions, the review-focus `file:line` links, and the explain-back questions.
  Then wait. Valid replies are `approved`, `change: …`, and `explain: …`.
- **Test-first for the domain:** in Phase 1, propose the test case list and wait for approval
  before writing any domain code.
- **Decisions:** any non-obvious choice goes into the PLAN.md decision log with status
  *Proposed*. Never mark a decision *Accepted*, and never tick an `[H]` or **Gate** item in
  TASKS.md. Only the human does that. Tick `[C]` items only after verifying them (build/tests
  pass), and update the "Current status" line in TASKS.md when a phase ends.
- **Never edit the HITL log** (PLAN.md §7) or the README's "How AI was used" section. The
  human writes those.
- **Explanations:** when asked `explain:`, answer in terms of this codebase (with file links)
  and include the tradeoff. If the honest answer is "this was arbitrary", say so.
- **Keep README.md current:** as part of each phase, update the feature status table, the run
  instructions, and the project structure to match what exists. Do this before presenting the
  gate packet.
- Keep diffs small and reviewable. No speculative code, no features outside PLAN.md scope.
- Every new domain rule gets a unit test. Every endpoint gets at least one happy-path and one
  failure-path integration test. On the frontend, logic (API client, validation, composables)
  gets unit tests. Components stay thin, and only `OperationPanel` gets a component test.
  No snapshot tests.
- Do not add dependencies beyond the stack above without asking.
- Do not commit. The human commits after each approved gate (Claude may suggest a message).
