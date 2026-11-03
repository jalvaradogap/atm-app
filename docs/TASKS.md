# TASKS.md: Execution checklist

This file is the **single progress tracker** for [PLAN.md](PLAN.md). The plan explains *what* and
*why*. This file tracks *done or not done*.

**Legend**
- `[C]`: Claude does it. Claude may tick it only after verifying it (build/tests pass).
- `[H]`: I (the human) do it. **Only I tick these.**
- **Gate** sections are always `[H]`. Claude never ticks them, and never starts the next phase
  until the gate is ticked and I say `approved`.

**Current status:** Gate 0, plan approval

---

## Prerequisites

- [x] P.1 `[H]` .NET SDK 10 installed (10.0.403)
- [x] P.2 `[H]` Node 22+ installed (26.13.0 via nvm)
- [x] P.3 `[H]` VS Code restarted; `node --version` works in its terminal
- [x] P.4 `[H]` `git init`
- [x] P.5 `[C]` Basic `README.md` (updated at the end of every phase)
- [ ] P.6 `[H]` First commit: `README.md`, `CLAUDE.md`, `docs/`

## Gate 0: Plan approval (10 min)

- [ ] G0.1 `[H]` Read [REQUIREMENT.md](REQUIREMENT.md) and PLAN.md §1–4
- [ ] G0.2 `[H]` Mark every decision (PLAN.md §6) as **Accepted** or **Changed**
- [ ] G0.3 `[H]` Record any change in the HITL log (PLAN.md §7)
- [ ] G0.4 `[H]` Commit: `docs: approve plan and decisions`

---

## Phase 0: Scaffold (15 min)

- [ ] T0.1 `[C]` Create `backend/Atm.slnx`
- [ ] T0.2 `[C]` Create the src projects:
  - `Atm.Domain`, `Atm.Application`, `Atm.Infrastructure` (class libraries)
  - `Atm.Api` (Web API with controllers)
- [ ] T0.3 `[C]` Create the test projects `Atm.Domain.Tests` and `Atm.Api.Tests` (xUnit), with the assertion library from decision #12
- [ ] T0.4 `[C]` Add project references that follow the dependency rule:
  - Domain references nothing
  - Application → Domain
  - Infrastructure → Application, Domain
  - Api → Application, Infrastructure
- [ ] T0.5 `[C]` `backend/Directory.Build.props` with:
  - `net10.0`
  - nullable enabled
  - implicit usings
  - `TreatWarningsAsErrors`
- [ ] T0.6 `[C]` Remove the template boilerplate: `Class1.cs`, `UnitTest1.cs`, `WeatherForecast*`
- [ ] T0.7 `[C]` Scaffold `frontend/` with Vite `vue-ts` and remove the demo components
- [ ] T0.8 `[C]` Root `.gitignore` covering `bin/`, `obj/`, `node_modules/`, `dist/`, `*.db`, and `.vs/`
- [ ] T0.9 `[C]` Verify that `dotnet build` and `npm run build` both succeed
- [ ] T0.10 `[C]` Present the gate packet

### Gate 1: Structure (5 min)
- [ ] G1.1 `[H]` Project references match the dependency rule
- [ ] G1.2 `[H]` `dotnet build` and `npm run dev` work on my machine
- [ ] G1.3 `[H]` Review checklist done (PLAN.md §3)
- [ ] G1.4 `[H]` HITL log updated, then commit: `chore: scaffold solution and frontend`

---

## Phase 1: Domain, test-first (40 min)

- [ ] T1.1 `[C]` Propose the domain test case list (names plus expected outcomes, no code)

### Mini-gate 1a: Test spec (5 min)
- [ ] M1.1 `[H]` Approve, add, or remove test cases

- [ ] T1.2 `[C]` `TransactionType` enum: `Deposit`, `Withdrawal`, `TransferIn`, `TransferOut`
- [ ] T1.3 `[C]` `Transaction` entity: immutable, created only through `Account`
- [ ] T1.4 `[C]` `DomainException` base class, plus:
  - `InvalidAmountException`
  - `InsufficientFundsException`
  - `SameAccountTransferException`
  - `AccountNotFoundException`
- [ ] T1.5 `[C]` Amount validation: > 0, ≤ 2 decimals, ≤ 1,000,000
- [ ] T1.6 `[C]` `Account.Deposit`
- [ ] T1.7 `[C]` `Account.Withdraw` (no overdraft)
- [ ] T1.8 `[C]` `Account.TransferOut` / `Account.TransferIn` with a shared `TransferId`
- [ ] T1.9 `[C]` Implement the approved tests, all green
- [ ] T1.10 `[C]` Present the gate packet

### Gate 2: Domain (10 min)
- [ ] G2.1 `[H]` I can explain why the rules live on `Account` and not in a service
- [ ] G2.2 `[H]` Break the overdraft check, see a test fail, then revert
- [ ] G2.3 `[H]` Review checklist done (PLAN.md §3)
- [ ] G2.4 `[H]` HITL log updated, then commit: `feat(domain): accounts, ledger and invariants`

---

## Phase 2: Application + Infrastructure (30 min)

- [ ] T2.1 `[C]` DTOs (`AccountDto`, `TransactionDto`, `PagedResult<T>`, `TransferResultDto`), plus static mapping
- [ ] T2.2 `[C]` `IAccountRepository` interface in Application
- [ ] T2.3 `[C]` `AccountService` queries: `GetAccounts`, `GetAccount`, `GetTransactions` (paged, newest first)
- [ ] T2.4 `[C]` `AccountService` commands: `Deposit`, `Withdraw`
- [ ] T2.5 `[C]` `AccountService.Transfer`: same-account check, both legs, a single `SaveChangesAsync`
- [ ] T2.6 `[C]` `AtmDbContext` and entity configurations
- [ ] T2.7 `[C]` Value converter that stores `decimal` as integer cents
- [ ] T2.8 `[C]` `Version` concurrency token, incremented on every balance change
- [ ] T2.9 `[C]` `AccountRepository` implementation
- [ ] T2.10 `[C]` Seed Checking $1,000 and Savings $5,000 via `EnsureCreated`, plus an `AddInfrastructure()` DI extension
- [ ] T2.11 `[C]` Test that the cents converter round-trips 0.10 and 999.99
- [ ] T2.12 `[C]` Build and tests green, then present the gate packet

### Gate 3: Persistence (10 min)
- [ ] G3.1 `[H]` I can explain how the transfer is atomic (single `SaveChangesAsync`)
- [ ] G3.2 `[H]` I can explain why money is stored as cents and how the converter works
- [ ] G3.3 `[H]` I can explain how `Version` detects a concurrent update
- [ ] G3.4 `[H]` Review checklist done (PLAN.md §3)
- [ ] G3.5 `[H]` HITL log updated, then commit: `feat: application services and SQLite persistence`

---

## Phase 3: API (30 min)

- [ ] T3.1 `[C]` Request records with DataAnnotations: `AmountRequest` and `TransferRequest`
- [ ] T3.2 `[C]` `AccountsController`:
  - `GET` list
  - `GET` by id
  - `GET` transactions
  - `POST` deposits
  - `POST` withdrawals
- [ ] T3.3 `[C]` `TransfersController`: `POST /api/transfers`
- [ ] T3.4 `[C]` `GlobalExceptionHandler`: maps exceptions to `ProblemDetails`, as 404 / 422 / 500
- [ ] T3.5 `[C]` `DbUpdateConcurrencyException` → 409
- [ ] T3.6 `[C]` `Program.cs` wiring:
  - OpenAPI
  - CORS for `http://localhost:5173`
  - port 5080
  - DB initialization at startup
- [ ] T3.7 `[C]` `Atm.Api.http` with one request per endpoint and per error case
- [ ] T3.8 `[C]` Test factory: `WebApplicationFactory` with an in-memory SQLite connection per test
- [ ] T3.9 `[C]` Integration tests:
  - [ ] list accounts → 200, 2 accounts
  - [ ] deposit → 200, balance updated
  - [ ] withdraw more than the balance → 422, balance unchanged
  - [ ] transfer → both balances updated, 2 entries sharing a `TransferId`
  - [ ] unknown account → 404
  - [ ] invalid body (missing or negative amount) → 400
- [ ] T3.10 `[C]` Build and tests green, then present the gate packet

### Gate 4: API contract (10 min)
- [ ] G4.1 `[H]` Ran every request in `Atm.Api.http`; status codes and bodies match PLAN.md §4
- [ ] G4.2 `[H]` An unexpected exception returns a generic 500 with no stack trace
- [ ] G4.3 `[H]` Review checklist done (PLAN.md §3)
- [ ] G4.4 `[H]` HITL log updated, then commit: `feat(api): endpoints, validation and error contract`

---

## Phase 4a: Frontend data layer (20 min)

- [ ] T4a.1 `[C]` `src/types/`: DTO types and `ProblemDetails`
- [ ] T4a.2 `[C]` `src/api/atmClient.ts`: a typed `fetch` wrapper, plus an `ApiError` built from problem+json
- [ ] T4a.3 `[C]` `src/composables/useAccounts.ts`: state, selection, loading/error, and actions that refresh the data
- [ ] T4a.4 `[C]` Vite proxy from `/api` to `http://localhost:5080`
- [ ] T4a.5 `[C]` `npm run build` (type check) passes, then present the gate packet

### Gate 5: Data layer (5 min)
- [ ] G5.1 `[H]` I can trace a single action: click → API call → state update
- [ ] G5.2 `[H]` Review checklist done (PLAN.md §3)
- [ ] G5.3 `[H]` HITL log updated, then commit: `feat(web): api client and state composable`

---

## Phase 4b: Frontend UI (30 min)

- [ ] T4b.1 `[C]` `AccountCard`: name, masked number, balance, and the selected state
- [ ] T4b.2 `[C]` `OperationPanel`:
  - Deposit / Withdraw / Transfer tabs
  - amount validation
  - submit disabled while pending
- [ ] T4b.3 `[C]` `TransactionHistory`: type, a signed and colored amount, the balance after, the date, and paging
- [ ] T4b.4 `[C]` `StatusMessage`: success and error feedback
- [ ] T4b.5 `[C]` `App.vue` layout, plus CSS variables, a mobile-friendly width, and `Intl.NumberFormat` currency
- [ ] T4b.6 `[C]` `npm run build` passes, then present the gate packet

### Gate 6: UI (5 min)
- [ ] G6.1 `[H]` Deposit, withdraw, and transfer all work, and both balances and the history refresh
- [ ] G6.2 `[H]` Each error path shows a clear message:
  - [ ] overdraft
  - [ ] amount 0
  - [ ] 3 decimals
  - [ ] transfer to the same account (should not even be selectable)
  - [ ] backend stopped
- [ ] G6.3 `[H]` Review checklist done (PLAN.md §3)
- [ ] G6.4 `[H]` HITL log updated, then commit: `feat(web): ATM user interface`

---

## Phase 5: Documentation and final review (20 min)

- [ ] T5.1 `[C]` Finalize `README.md`:
  - how to run
  - architecture
  - tradeoffs
  - what I'd do with more time
- [ ] T5.2 `[H]` Write the README's "How AI was used" section from the HITL log
- [ ] T5.3 `[C]` Remove unused code, then run `dotnet test` and `npm run build` one last time
- [ ] T5.4 `[C]` Present the gate packet

### Gate 7: Ready to submit
- [ ] G7.1 `[H]` A fresh clone builds and runs by following only the README
- [ ] G7.2 `[H]` I can answer every explain-back question without notes
- [ ] G7.3 `[H]` Final commit and submit

---

## Stretch (only if time remains; first to be cut)

- [ ] S.1 `[C]` Vitest tests for `atmClient` error parsing and `useAccounts`
- [ ] S.2 `[C]` A concurrency integration test (two simultaneous withdrawals, one gets 409)
- [ ] S.3 `[C]` Transaction descriptions shown in the UI
