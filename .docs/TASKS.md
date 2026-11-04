# TASKS.md: Execution checklist

This file is the **single progress tracker** for [PLAN.md](PLAN.md). The plan explains *what* and
*why*. This file tracks *done or not done*.

**Legend**
- `[C]`: Claude does it. Claude may tick it only after verifying it (build/tests pass).
- `[H]`: I (the human) do it. **Only I tick these.**
- **Gate** sections are always `[H]`. Claude never ticks them, and never starts the next phase
  until the gate is ticked and I say `approved`.

**Current status:** Done. All phases and gates are complete (Gates 3 to 7 approved by the human).

---

## Prerequisites

- [x] P.1 `[H]` .NET SDK 10 installed (10.0.403)
- [x] P.2 `[H]` Node 22+ installed (26.13.0 via nvm)
- [x] P.3 `[H]` VS Code restarted; `node --version` works in its terminal
- [x] P.4 `[H]` `git init`
- [x] P.5 `[C]` Basic `README.md` (updated at the end of every phase)
- [x] P.6 `[H]` First commit: `README.md`, `CLAUDE.md`, `docs/`

## Gate 0: Plan approval (10 min)

- [x] G0.1 `[H]` Read [REQUIREMENT.md](REQUIREMENT.md) and PLAN.md §1–4
- [x] G0.2 `[H]` Mark every decision (PLAN.md §6) as **Accepted** or **Changed**
- [x] G0.3 `[H]` Record any change in the HITL log (PLAN.md §7)
- [x] G0.4 `[H]` Commit: `docs: approve plan and decisions`

---

## Phase 0: Scaffold (15 min)

- [x] T0.1 `[C]` Create `backend/Atm.slnx`
- [x] T0.2 `[C]` Create the src projects:
  - `Atm.Domain`, `Atm.Application`, `Atm.Infrastructure` (class libraries)
  - `Atm.Api` (Web API with controllers)
- [x] T0.3 `[C]` Create the test projects `Atm.Domain.Tests` and `Atm.Api.Tests` (xUnit), with the assertion library from decision #12
- [x] T0.4 `[C]` Add project references that follow the dependency rule:
  - Domain references nothing
  - Application → Domain
  - Infrastructure → Application, Domain
  - Api → Application, Infrastructure
- [x] T0.5 `[C]` `backend/Directory.Build.props` with:
  - `net10.0`
  - nullable enabled
  - implicit usings
  - `TreatWarningsAsErrors`
- [x] T0.6 `[C]` Remove the template boilerplate: `Class1.cs`, `UnitTest1.cs`, `WeatherForecast*`
- [x] T0.7 `[C]` Scaffold `frontend/` with `create-vite@9.3.3`'s `vue-ts` template, keep its versions (decision #13), and remove the demo components
- [x] T0.7b `[C]` Add Tailwind 4 (decision #16): install `tailwindcss` and `@tailwindcss/vite`, register the plugin in `vite.config.ts`, add `@import "tailwindcss";` to `src/style.css`, and confirm one utility class renders
- [x] T0.8 `[C]` Root `.gitignore` covering `bin/`, `obj/`, `node_modules/`, `dist/`, `*.db`, and `.vs/`
- [x] T0.9 `[C]` Verify that `dotnet build` and `npm run build` both succeed, and that `package-lock.json` is present (to be committed)
- [x] T0.10 `[C]` Present the gate packet

### Gate 1: Structure (5 min)
- [x] G1.1 `[H]` Project references match the dependency rule
- [x] G1.2 `[H]` `dotnet build` and `npm run dev` work on my machine
- [x] G1.3 `[H]` Review checklist done (PLAN.md §3)
- [x] G1.4 `[H]` HITL log updated, then commit: `chore: scaffold solution and frontend`

---

## Phase 1: Domain, test-first (40 min)

- [x] T1.1 `[C]` Propose the domain test case list (names plus expected outcomes, no code)

### Mini-gate 1a: Test spec (5 min)
- [x] M1.1 `[H]` Approve, add, or remove test cases

- [x] T1.2 `[C]` `TransactionType` enum: `Deposit`, `Withdrawal`, `TransferIn`, `TransferOut`
- [x] T1.3 `[C]` `Transaction` entity: immutable, created only through `Account`
- [x] T1.4 `[C]` `DomainException` base class, plus:
  - `InvalidAmountException`
  - `InsufficientFundsException`
  - `SameAccountTransferException`
  - `AccountNotFoundException`
- [x] T1.5 `[C]` Amount validation: > 0, ≤ 2 decimals, ≤ 1,000,000
- [x] T1.6 `[C]` `Account.Deposit`
- [x] T1.7 `[C]` `Account.Withdraw` (no overdraft)
- [x] T1.8 `[C]` `Account.TransferOut` / `Account.TransferIn` with a shared `TransferId`
- [x] T1.9 `[C]` Implement the approved tests, all green
- [x] T1.10 `[C]` Present the gate packet

### Gate 2: Domain (10 min)
- [x] G2.1 `[H]` I can explain why the rules live on `Account` and not in a service
- [x] G2.2 `[H]` Break the overdraft check, see a test fail, then revert
- [x] G2.3 `[H]` Review checklist done (PLAN.md §3)
- [x] G2.4 `[H]` HITL log updated, then commit: `feat(domain): accounts, ledger and invariants`

---

## Phase 2: Application + Infrastructure (30 min)

- [x] T2.1 `[C]` DTOs (`AccountDto`, `TransactionDto`, `PagedResult<T>`, `TransferResultDto`), plus static mapping
- [x] T2.2 `[C]` `IAccountRepository` interface in Application
- [x] T2.3 `[C]` `AccountService` queries: `GetAccounts`, `GetAccount`, `GetTransactions` (paged, newest first)
- [x] T2.4 `[C]` `AccountService` commands: `Deposit`, `Withdraw`
- [x] T2.5 `[C]` `AccountService.Transfer`: same-account check, both legs, a single `SaveChangesAsync`
- [x] T2.6 `[C]` `AtmDbContext` and entity configurations
- [x] T2.7 `[C]` Value converter that stores `decimal` as integer cents
- [x] T2.8 `[C]` `Version` concurrency token, incremented on every balance change
- [x] T2.9 `[C]` `AccountRepository` implementation
- [x] T2.10 `[C]` Seed Checking $1,000 and Savings $5,000 via `EnsureCreated`, plus an `AddInfrastructure()` DI extension
- [x] T2.11 `[C]` Test that the cents converter round-trips 0.10 and 999.99
- [x] T2.12 `[C]` Build and tests green, then present the gate packet

### Gate 3: Persistence (10 min)
- [x] G3.1 `[H]` I can explain how the transfer is atomic (single `SaveChangesAsync`)
- [x] G3.2 `[H]` I can explain why money is stored as cents and how the converter works
- [x] G3.3 `[H]` I can explain how `Version` detects a concurrent update
- [x] G3.4 `[H]` Review checklist done (PLAN.md §3)
- [x] G3.5 `[H]` HITL log updated, then commit: `feat: application services and SQLite persistence`

---

## Phase 3: API (30 min)

- [x] T3.1 `[C]` Request records with DataAnnotations: `AmountRequest` and `TransferRequest`
- [x] T3.2 `[C]` `AccountsController`:
  - `GET` list
  - `GET` by id
  - `GET` transactions
  - `POST` deposits
  - `POST` withdrawals
- [x] T3.3 `[C]` `TransfersController`: `POST /api/transfers`
- [x] T3.4 `[C]` `GlobalExceptionHandler`: maps exceptions to `ProblemDetails`, as 404 / 422 / 500
- [x] T3.5 `[C]` `DbUpdateConcurrencyException` → 409
- [x] T3.6 `[C]` `Program.cs` wiring:
  - OpenAPI
  - CORS for `http://localhost:5173`
  - port 5080
  - DB initialization at startup
- [x] T3.7 `[C]` `Atm.Api.http` with one request per endpoint and per error case
- [x] T3.8 `[C]` Test factory: `WebApplicationFactory` with an in-memory SQLite connection per test
- [x] T3.9 `[C]` Integration tests:
  - [x] list accounts → 200, 2 accounts
  - [x] deposit → 200, balance updated
  - [x] withdraw more than the balance → 422, balance unchanged
  - [x] transfer → both balances updated, 2 entries sharing a `TransferId`
  - [x] unknown account → 404
  - [x] invalid body (missing or negative amount) → 400
- [x] T3.10 `[C]` Build and tests green, then present the gate packet

### Gate 4: API contract (10 min)
- [x] G4.1 `[H]` Ran every request in `Atm.Api.http`; status codes and bodies match PLAN.md §4
- [x] G4.2 `[H]` An unexpected exception returns a generic 500 with no stack trace
- [x] G4.3 `[H]` Review checklist done (PLAN.md §3)
- [x] G4.4 `[H]` HITL log updated, then commit: `feat(api): endpoints, validation and error contract`

---

## Phase 4a: Frontend data layer + unit tests (30 min)

- [x] T4a.1 `[C]` `src/types/`: DTO types and `ProblemDetails`
- [x] T4a.2 `[C]` `src/api/atmClient.ts`: a typed `fetch` wrapper, plus an `ApiError` built from problem+json
- [x] T4a.3 `[C]` `src/utils/amount.ts`: pure `validateAmount()` (> 0, ≤ 2 decimals, ≤ max)
- [x] T4a.4 `[C]` `src/composables/useAccounts.ts`: state, selection, loading/error, and actions that refresh the data
- [x] T4a.5 `[C]` Vite proxy from `/api` to `http://localhost:5080`
- [x] T4a.6 `[C]` Vitest setup: install `vitest`, `@vue/test-utils`, and `jsdom` (decision #14), configure the jsdom environment, and add the `npm run test` script
- [x] T4a.7 `[C]` Unit tests, with `fetch` / the API module mocked:
  - [x] `atmClient`: success returns typed data
  - [x] `atmClient`: a 422 problem+json becomes an `ApiError` with status and detail
  - [x] `atmClient`: a network failure becomes a "cannot reach server" error
  - [x] `atmClient`: a non-JSON 500 becomes a generic error
  - [x] `validateAmount`: valid, 0, negative, 3 decimals, empty, above max
  - [x] `useAccounts`: a successful deposit refreshes balances and history
  - [x] `useAccounts`: a failed operation sets the error and leaves balances unchanged
  - [x] `useAccounts`: `pending` toggles during an operation
- [x] T4a.8 `[C]` `npm run test` and `npm run build` (type check) pass, README updated, then present the gate packet

### Gate 5: Data layer + UX preview (8 min)
- [x] G5.1 `[H]` I can trace a single action: click → API call → state update
- [x] G5.2 `[H]` Break the `ApiError` parsing, see a test fail, then revert
- [x] G5.2b `[H]` Re-read [UX.md](UX.md) and approve or adjust it before Phase 4b starts
- [x] G5.3 `[H]` Review checklist done (PLAN.md §3)
- [x] G5.4 `[H]` HITL log updated, then commit: `feat(web): api client, state composable and unit tests`

---

## Phase 4b: Frontend UI + component test (35 min)

Acceptance criteria for this phase: [UX.md](UX.md).

- [x] T4b.1 `[C]` Theme tokens in an `@theme` block in `src/style.css` (accent, success, danger, surface). Formatting helpers in `src/utils/format.ts` (`Intl` currency and date, human-readable transaction labels).
- [x] T4b.2 `[C]` `AccountCard`: name, masked number, balance, and the selected state (border plus label)
- [x] T4b.3 `[C]` `OperationPanel`:
  - an accessible tablist for Deposit / Withdraw / Transfer
  - inline validation reasons, plus "Available: $X"
  - transfer shown as `From → To` with a swap button
  - submit labelled with the action and amount, disabled while invalid or pending, and Enter submits
- [x] T4b.4 `[C]` `TransactionHistory`: readable labels, signed and colored amounts, the balance after, local dates, the latest 20, and loading/empty states
- [x] T4b.5 `[C]` `StatusMessage` (`aria-live`) for success with new balances or an error, plus an unreachable-server banner with Retry
- [x] T4b.6 `[C]` `App.vue` layout: two columns that collapse to one below 768 px
- [x] T4b.7 `[C]` `OperationPanel` component test:
  - [x] submit disabled for an invalid amount
  - [x] transfer always targets the other account, and swap reverses it
  - [x] emits the operation with the parsed amount
- [x] T4b.8 `[C]` `npm run test` and `npm run build` pass, README updated, then present the gate packet

### Gate 6: UI (7 min)
- [x] G6.1 `[H]` Deposit, withdraw, and transfer all work, and both balances and the history refresh
- [x] G6.2 `[H]` Each error path shows a clear message:
  - [x] overdraft
  - [x] amount 0
  - [x] 3 decimals
  - [x] transfer to the same account (impossible by design, since only swap is available)
  - [x] backend stopped (banner + Retry)
- [x] G6.2b `[H]` Keyboard-only pass: Tab, switch tabs, submit with Enter, focus always visible
- [x] G6.2c `[H]` Phone width (375 px in devtools): single column, nothing cut off
- [x] G6.3 `[H]` Review checklist done (PLAN.md §3)
- [x] G6.4 `[H]` HITL log updated, then commit: `feat(web): ATM user interface`

---

## Phase 5: Documentation and final review (15 min)

- [x] T5.1 `[C]` Finalize `README.md`:
  - how to run
  - architecture
  - tradeoffs
  - what I'd do with more time
- [x] T5.2 `[H]` Write the README's "How AI was used" section from the HITL log
- [x] T5.3 `[C]` Remove unused code, then run `dotnet test`, `npm run test`, and `npm run build` one last time
- [x] T5.4 `[C]` Present the gate packet

### Gate 7: Ready to submit
- [x] G7.1 `[H]` A fresh clone builds and runs by following only the README
- [x] G7.2 `[H]` I can answer every explain-back question without notes
- [x] G7.3 `[H]` Final commit and submit

---

## Stretch (only if time remains; first to be cut)

Not planned: Playwright E2E tests (see decision #14). Listed in the README as a next step.

- [x] S.1 `[C]` A concurrency integration test (two simultaneous withdrawals, one gets 409) *(Done as a deterministic test of the 409 mapping, in `ConcurrencyEndpointTests`, rather than a timed race between two requests.)*
- [x] S.2 `[C]` Transaction descriptions shown in the UI
