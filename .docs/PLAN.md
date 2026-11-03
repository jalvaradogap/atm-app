# PLAN.md: ATM Application

## 1. Goal and scope

A web ATM for a single, unauthenticated user with **two accounts** (Checking and Savings).

**In scope (must have)**
- Deposit to an account
- Withdraw from an account (no overdraft)
- Transfer between the two accounts (atomic)
- Show current balances
- Show transaction history per account (newest first, paginated)
- Clear error messages for invalid operations

**Explicitly out of scope** (mentioned in the README as possible next steps)
- Authentication, multiple users, card/PIN flow
- Daily withdrawal limits, cash denominations, fees, currencies
- Idempotency keys for retried requests
- Docker, CI, deployment

## 2. Prerequisites

- .NET SDK 10 (installed: 10.0.403)
- Node.js 22 LTS or newer (installed: 26.13.0 via nvm, npm 11.20.0)
- Restart VS Code so `node` / `npm` are on the PATH in its terminals
- `git init` and a first commit (with CLAUDE.md and PLAN.md), so progress shows as a readable history

## 3. Human-in-the-loop workflow

**Principle:** I own the decisions and the final code. Claude proposes and implements in
small steps. Nothing moves past a gate until I approve it.

### Roles

| I (the human) | Claude |
|---------------|--------|
| Approve scope, architecture and every decision log entry | Proposes options and records them as *Proposed* |
| Define expected behavior (approve test cases before implementation) | Drafts test cases, then implements code to satisfy them |
| Review each diff, run the app, ask "why" | Builds, runs tests, explains changes, points at the riskiest code |
| Commit after each approved gate | Never commits unless asked; never ticks a gate checkbox |
| Write the README's "How AI was used" section and the HITL log in my own words | Drafts the rest of the README |

### The gate loop

```
 Claude implements phase ──► Claude presents gate packet ──► STOP
            ▲                                                 │
            │                     I review / run / question   │
            │                                                 ▼
            └──────── "change: …" / "explain: …" ◄──── my response
                                                              │
                                              "approved" ─────┴──► I commit ──► next phase
```

### Gate packet (what Claude presents at every gate)

1. **Summary:** what changed and why, in at most 10 lines, with links to the files.
2. **Verification:** `dotnet build` / `dotnet test` / `npm run build` results (pass and fail counts).
3. **New decisions:** any choice made during the phase that isn't in the decision log, added to
   it as *Proposed*.
4. **Review focus:** the 2–4 spots most worth my attention (riskiest or least obvious code),
   as `file:line` links.
5. **Explain-back questions:** 2–3 questions an interviewer might ask about this phase, without
   answers. I answer them; Claude corrects me if I get something wrong.

Then Claude **stops and waits**. Valid responses: `approved`, `change: <what>`, `explain: <what>`.

### My review checklist (every gate)

- I can explain what each new file is responsible for, and why it lives in that layer.
- Tests assert behavior (e.g., a failed withdrawal leaves the balance unchanged), not just "no exception".
- No dependency, abstraction or file I didn't expect.
- Matches the decision log and the API/error contract below.
- I ran it myself (tests, `.http` requests or the UI), not just read the summary.
- Anything I overrode or decided is recorded in the HITL log (section 7).

## 4. Architecture

```
 ┌─────────────── Vue 3 SPA ───────────────┐
 │ components → composables → api client   │
 └──────────────────┬──────────────────────┘
                    │  JSON over HTTP (/api/*)
 ┌──────────────────▼──────────────────────┐
 │ Atm.Api        controllers, validation, │
 │                ProblemDetails handler   │
 ├─────────────────────────────────────────┤
 │ Atm.Application AccountService (use     │
 │                cases), DTOs, interfaces │
 ├─────────────────────────────────────────┤
 │ Atm.Domain     Account, Transaction,    │
 │                rules, domain exceptions │
 ├─────────────────────────────────────────┤
 │ Atm.Infrastructure EF Core + SQLite     │
 └─────────────────────────────────────────┘
```

### Domain model

**Account**
- `Id` (Guid), `Name` ("Checking" / "Savings"), `Number` (masked display, e.g. `****1234`)
- `Balance` (decimal), `Version` (int, concurrency token)
- `Transactions` (collection)
- Methods:
  - `Deposit(amount, description?)`
  - `Withdraw(amount, description?)`
  - `TransferOut(...)` and `TransferIn(...)`, used only by the transfer use case

**Transaction** (ledger entry, immutable)
- `Id`, `AccountId`, `Type` (`Deposit | Withdrawal | TransferIn | TransferOut`)
- `Amount` (always positive), `BalanceAfter`, `OccurredAt` (UTC)
- `TransferId?` links the two legs of a transfer
- `Description`

**Invariants (enforced in Domain, unit tested)**
- Amount > 0, at most 2 decimal places, and ≤ a sanity max (e.g. 1,000,000)
- A withdrawal or outgoing transfer can't exceed the balance
- A transfer can't go to the same account
- Every balance change produces exactly one ledger entry, so the balance always equals the sum
  of the ledger

### REST API

| Method | Route                                         | Body                                  | Success |
|--------|-----------------------------------------------|---------------------------------------|---------|
| GET    | `/api/accounts`                               | –                                     | 200 `AccountDto[]` |
| GET    | `/api/accounts/{id}`                          | –                                     | 200 `AccountDto` |
| GET    | `/api/accounts/{id}/transactions?page=1&pageSize=20` | –                              | 200 `PagedResult<TransactionDto>` |
| POST   | `/api/accounts/{id}/deposits`                 | `{ amount, description? }`            | 200 `AccountDto` |
| POST   | `/api/accounts/{id}/withdrawals`              | `{ amount, description? }`            | 200 `AccountDto` |
| POST   | `/api/transfers`                              | `{ fromAccountId, toAccountId, amount, description? }` | 200 `TransferResultDto` (both balances) |

**Error contract** (RFC 7807 `application/problem+json`)

| Case                                       | Status |
|--------------------------------------------|--------|
| Malformed body / failed model validation   | 400 |
| Account not found                          | 404 |
| Concurrency conflict                       | 409 |
| Insufficient funds, same-account transfer, invalid amount | 422 |
| Unhandled                                  | 500 (generic message, details only logged) |

## 5. Implementation phases and gates (~4 h, including review time)

Review time is part of the budget on purpose: it's where I build the understanding I'll need
to discuss the code.

> **Progress is tracked in [TASKS.md](TASKS.md)**, not here. This section describes the phases.
> TASKS.md breaks them into checkable items, each tagged with an owner (`[C]` Claude / `[H]` me).

### Gate 0: Plan approval (10 min), before any code
- I read sections 1–4 and 6, and edit anything I disagree with
- Every decision log entry is marked **Accepted** or **Changed** by me
- Changes recorded in the HITL log, then I commit

### Phase 0: Scaffold (15 min)
- `backend/`: create the `Atm.slnx` solution, the 4 src projects, the 2 test projects, and the project references
- Enable nullable, `TreatWarningsAsErrors`, and shared settings in `Directory.Build.props`
- `frontend/`: `npm create vite@latest frontend -- --template vue-ts`
- Add a `.gitignore` covering `bin/`, `obj/`, `node_modules/`, `*.db`, and `dist/`

**Gate 1: Structure (5 min)**
- Project references match the dependency rule (Domain references nothing)
- `dotnet build` and `npm run dev` both work on my machine

### Phase 1: Domain, test-first (40 min)
- **1a. Spec:** Claude lists the domain test cases as names plus expected outcomes, without code yet.
  I add, remove, or change cases. **(mini-gate, 5 min)**
- 1b. Implement `Account`, `Transaction`, `TransactionType`, and the domain exceptions
  (shared base `DomainException`)
- 1c. Implement the approved tests. Must cover:
  - deposit (happy path, zero, negative, 3 decimals)
  - withdraw (happy path, overdraft, exact balance)
  - balance unchanged after a rejected operation
  - ledger entries and `BalanceAfter` values

**Gate 2: Domain (10 min)**
- I can explain why the rules live on `Account` and not in a service
- I deliberately break one rule (e.g., remove the overdraft check) and confirm a test fails

### Phase 2: Application + Infrastructure (30 min)
- `IAccountRepository` (get by id with/without transactions, list, paged transactions, `SaveChangesAsync`)
- `AccountService` with:
  - `GetAccounts`
  - `GetAccount`
  - `GetTransactions`
  - `Deposit`
  - `Withdraw`
  - `Transfer`
- DTOs and their mapping, written as plain static methods
- `AtmDbContext` with entity configurations, cents value converter, and concurrency token
- Seed two accounts on startup (Checking $1,000, Savings $5,000) using `EnsureCreated`

**Gate 3: Persistence (10 min).** Focus on the three things most likely to be asked about:
- Transfer atomicity: both legs saved in a single `SaveChangesAsync`
- The cents value converter round-trips correctly (e.g., 0.10, 999.99)
- How the `Version` token detects a concurrent update

### Phase 3: API (30 min)
- `AccountsController` and `TransfersController`
- Request records with DataAnnotations validation
- `GlobalExceptionHandler : IExceptionHandler` that maps exceptions to `ProblemDetails`
- Map `DbUpdateConcurrencyException` to 409
- OpenAPI document (built-in `Microsoft.AspNetCore.OpenApi`), CORS for the Vite dev origin
- `Atm.Api.http` file with one request per endpoint and per error case, so I can explore by hand
- Integration tests (`WebApplicationFactory`, SQLite in-memory connection per test). Cover:
  - list accounts
  - deposit
  - overdraft → 422
  - transfer updates both balances and writes 2 linked entries
  - unknown account → 404
  - invalid body → 400

**Gate 4: API contract (10 min)**
- I run every request in `Atm.Api.http` and check each status code and response body against section 4
- An unexpected exception returns a generic 500 with no stack trace

### Phase 4a: Frontend data layer + unit tests (30 min)
- `types/` with TS types for the DTOs and `ProblemDetails`
- `api/atmClient.ts`: typed `fetch` wrapper that turns problem+json into an `ApiError`
- `utils/amount.ts`: a pure `validateAmount()` function (> 0, ≤ 2 decimals, ≤ max) that mirrors
  the domain rules, so the UI gives instant feedback. The backend stays the authority.
- `composables/useAccounts.ts`: accounts state, selected account, loading/error, and actions that refresh balances and history
- Vite proxy `/api` → backend
- Vitest setup (`jsdom` environment) and `npm run test`
- Unit tests (with `fetch` / the API module mocked, no backend needed):
  - `atmClient`:
    - success returns typed data
    - a 422 problem+json becomes an `ApiError` with status and detail
    - a network failure becomes a "cannot reach server" error
    - a non-JSON 500 becomes a generic error
  - `validateAmount`: valid, 0, negative, 3 decimals, empty, above max
  - `useAccounts`:
    - a successful deposit refreshes balances and history
    - a failed operation sets the error and leaves balances unchanged
    - `pending` toggles

**Gate 5: Data layer + UX preview (8 min)**
- I can trace one action from button click to HTTP call to state update
- `npm run test` passes. I break the `ApiError` parsing and see a test fail.
- I re-read [UX.md](UX.md) and approve or adjust the layout and interactions *before* Phase 4b builds them.

### Phase 4b: Frontend UI + component test (35 min)
Implements [UX.md](UX.md). The spec is the acceptance criteria for this phase.
- Components:
  - `AccountCard`: name, masked number, balance, selected state (border plus label); click to select
  - `OperationPanel`:
    - Deposit / Withdraw / Transfer tabs (accessible tablist)
    - amount input with inline validation reasons and "Available: $X"
    - transfer shown as `From → To` with a swap button (no dropdown)
    - submit button labelled with the action and amount, disabled while invalid or pending
  - `TransactionHistory`: human-readable type, signed and colored amount, balance after, local date. Shows the latest 20, with loading and empty states.
  - `StatusMessage`: success (with new balances) and error, in an `aria-live` region. Unreachable server shows a banner with Retry.
- Styling: Tailwind utilities with the theme tokens from UX.md §4, `tabular-nums` for money, `Intl` formatting, and a two-column layout (`md:grid-cols-2`) that collapses to one column below 768 px
- Component test (`@vue/test-utils`) for `OperationPanel`:
  - submit disabled for an invalid amount
  - transfer always targets the other account, and swap reverses the direction
  - emits the operation with the parsed amount

**Gate 6: UI (7 min)**
- I do every operation and every error path in the browser (overdraft, 0, 3 decimals, backend stopped)
- Keyboard-only pass: Tab through the page, switch tabs, submit with Enter, focus always visible
- Phone width (devtools, 375 px): single column, nothing cut off
- `npm run test` and `npm run build` both pass

### Phase 5: Documentation and final review (15 min)
- Claude drafts `README.md` covering:
  - how to run
  - architecture overview
  - tradeoffs (from the decision log)
  - what I'd do with more time
- **I write** the "How AI was used" section from the HITL log
- Final self-review: unused code removed, all tests pass

**Gate 7: Ready to submit**
- Clean clone builds and runs by following only the README
- I can answer every explain-back question from the earlier gates without notes

**Cut line if time runs short:** these get dropped first, in this order:
1. dark mode (UX.md §4)
2. the `OperationPanel` component test
3. transaction descriptions

Never cut: domain tests, API integration tests, the `atmClient` / `validateAmount` unit tests,
error handling, atomic transfers, and **the gates themselves**. Shortening a review defeats the purpose of the approach.

## 6. Decision log

Claude proposed every entry below. All of #1–16 were accepted at Gate 0.
Entries added in later phases go through the next gate.

| # | Decision | Alternative considered | Why | Status |
|---|----------|------------------------|-----|--------|
| 1 | Layered (Domain / Application / Infrastructure / Api) | Single project with folders | Makes separation of concerns explicit and enforced by project references. Each project stays small, so the overhead is low. | Accepted |
| 2 | Rich domain model (rules inside `Account`) | Anemic entities + service logic | Invariants live in one place and are unit-testable without a DB. | Accepted |
| 3 | EF Core + SQLite (file) | EF InMemory provider / static dictionary | Real persistence and real DB transactions for an atomic transfer. Zero setup for the reviewer. The InMemory provider doesn't enforce transactions or concurrency faithfully. | Accepted |
| 4 | `EnsureCreated` instead of migrations | EF migrations | Single fixed schema in an exercise. Migrations would be the first change for a real app. | Accepted |
| 5 | Money stored as integer cents | SQLite TEXT decimal / REAL | SQLite has no decimal type. REAL loses precision. Integer cents are exact and sortable. | Accepted |
| 6 | Exceptions for domain rule violations + global handler | Result<T> pattern | Less ceremony in a small codebase. One central mapping to HTTP. Result<T> would be preferable if failures were very frequent or were composed. | Accepted |
| 7 | Optimistic concurrency (`Version` token) | Pessimistic locks / none | Cheap protection against lost updates (e.g., two tabs). Surfaces as 409. | Accepted |
| 8 | Ledger entry per balance change, transfer legs linked by `TransferId` | Single transfer row | Keeps per-account history simple to query and auditable. | Accepted |
| 9 | No MediatR / AutoMapper / repository-per-entity generics | Typical "enterprise" template | Scope doesn't justify them. Fewer moving parts to explain. | Accepted |
| 10 | Vue composable for state instead of Pinia | Pinia store | One screen and one state slice, so a composable is enough. Pinia would be the move if more views appear. | Accepted |
| 11 | Human-in-the-loop gates after every phase | Generate the whole app, then review | Keeps diffs small enough to understand fully, and catches wrong assumptions early, when they're cheap to fix. Costs ~45 min of the budget. | Accepted |
| 12 | Test assertions: FluentAssertions pinned to **7.x** | FluentAssertions 8.x / Shouldly / plain xUnit `Assert` | v8+ moved to a commercial license (Xceed). 7.x is the last Apache-2.0 version and has the same API. Shouldly is the fallback if pinning an old major isn't wanted. | Accepted |
| 13 | Frontend versions: Vue **3.5.x**, Vite **8.x**, TypeScript **6.0.x**, as scaffolded by `create-vite` 9.3, with `package-lock.json` committed | Vue 3.6 RC / TypeScript 7.0 (native Go compiler) | Use only stable releases. `vue-tsc` type-checks `.vue` files via the JS TypeScript API, so moving to TS 7 risks breaking the build for no benefit in this scope. The lockfile makes installs reproducible for the reviewer. | Accepted |
| 14 | Frontend tests: **Vitest 5 + `@vue/test-utils` 2 + jsdom**. Unit tests on the API client, amount validation and the composable, plus one component test. No E2E. | No FE tests / Playwright E2E / snapshot tests | Testing goes where the logic is: error parsing, validation, and state transitions. Components stay thin, so one component test is enough. Vitest reuses the Vite config. Playwright would cover full user flows but costs setup time the timebox doesn't have, so it's listed as a next step. Snapshot tests would break on every markup change without catching real bugs. | Accepted |
| 15 | Lightweight UX spec ([UX.md](UX.md)): error prevention (swap button instead of a destination picker, inline validation), clear outcome messages, and a baseline of accessibility | No design thought at all | Design isn't scored first, but clarity and error prevention are part of a correct ATM. Paid for by showing the latest 20 history entries instead of "Load more". | Accepted |
| 16 | Styling with **Tailwind CSS 4** (`@tailwindcss/vite`), with design tokens in an `@theme` block | Hand-written CSS with `:root` tokens / component library (Vuetify, PrimeVue) | Utility classes give a consistent spacing and colour scale, responsive layout (`md:`) and dark mode (`dark:`) almost for free, which saves time on a non-scored area. Unlike a component library, all markup stays explicit and explainable. Cost: 2 build-time dev dependencies, plus long class lists in templates, kept in check by extracting small components instead of overusing `@apply`. | Accepted |
| 17 | Shared build settings in `backend/Directory.Build.props` (`net10.0`, nullable, implicit usings, `TreatWarningsAsErrors`). `Atm.Domain.Tests` references only `Atm.Domain`, and `Atm.Api.Tests` references `Atm.Api` (needed later for `WebApplicationFactory`) | Repeating these settings in each csproj | One place to change the target framework and warning policy, so projects can't drift apart. Warnings as errors stop unused code and nullability gaps from piling up. | Proposed |
| 18 | FluentAssertions pinned to the exact version `7.2.0` | Floating range `7.*` | An exact pin keeps restores reproducible for the reviewer. Still within the 7.x line from decision #12. | Proposed |
| 19 | Amounts with more than 2 decimals (e.g. 10.001) are rejected with `InvalidAmountException`, not rounded. Trailing zeros (5.00) are fine | Silent rounding to 2 decimals | The user typed a value the bank can't hold. Rejecting makes the error visible, which fits the "clear error messages" requirement. | Proposed |
| 20 | The 1,000,000 cap applies to each operation amount, not to the balance | Cap on the balance | Matches the plan's wording (a sanity max on the amount). A balance cap would need a rule for deposits that cross it. | Proposed |
| 21 | Transfer legs are two methods on `Account`: `TransferOut(toAccountId, transferId, amount, description?)` and `TransferIn(fromAccountId, transferId, amount, description?)`. The caller supplies the `TransferId`, and `TransferOut` owns the same-account check | One `Account.TransferTo(destination, ...)` that changes both accounts | Each method changes only its own account. The pairing and the single `SaveChangesAsync` (T2.5) stay in the application layer, where persistence lives. | Proposed |
| 22 | New accounts open at zero via `Account.Open(name, number)`. Seeding then makes opening deposits | An opening-balance parameter | Every balance change goes through the ledger, so the ledger always sums to the balance. There is no unexplained starting balance. | Proposed |
| 23 | `OccurredAt` is set from `DateTime.UtcNow` inside the domain. Tests assert it falls between a before and after timestamp | Inject `TimeProvider` into every method | No extra parameter on every call. Tradeoff: tests check a time window, not an exact value. `TimeProvider` is the move if exact timestamps are ever needed. | Proposed |

## 7. HITL log

Filled in by me during the gates. It's the evidence of where I steered the AI, and the source for the
README's "How AI was used" section.

| Gate | What Claude proposed | What I decided / changed | Why |
|------|----------------------|--------------------------|-----|
| 0 | (nothing yet; this was my input) | Chose .NET + Vue.js and building from scratch before any planning started | Stack I'm productive in and can defend |
| 0 | A plan with only a light "review each phase" step | Asked for an explicit human-in-the-loop process, which added gates, a gate packet, the review checklist, and `[C]`/`[H]` ownership | I must understand and defend every line, so review points have to be enforced, not optional |
| 0 | Plan and requirements at the repo root | Moved the docs into `docs/` and added REQUIREMENT.md with the original statement, word for word | Keeps the requirements traceable as the source of truth for scope |
| 0 | Progress checkboxes inside PLAN.md | Asked for a separate TASKS.md, which became the only progress tracker | Separate *what/why* (plan) from *done or not done* (tasks), and avoid two lists drifting apart |
| 0 | README written only in the final phase | Created the README before the first commit, updated every phase | The repo should explain itself at every commit |
| 0 | Node version documented, frontend versions not | Asked about the Vue version, which led to decision #13: stable Vue 3.5 / Vite 8 / TS 6.0, plus a committed lockfile | Avoid prerelease versions (Vue 3.6 RC) and the TS 7 / `vue-tsc` risk. Reproducible installs. |
| 0 | Frontend tests only as optional stretch work | Made FE tests part of the plan (decision #14), focused on the API client, validation, and the composable | Testing only the backend would look unbalanced; FE logic deserves the same care |
| 0 | Only "simple, clean styling" | Asked for a UX proposal, which produced UX.md (decision #15): error prevention, clear outcomes, accessibility basics | The requirement says "good design is still good design" |
| 0 | Plain CSS, with Tailwind wrongly grouped with component libraries | Chose Tailwind 4 (decision #16) | Faster on a non-scored area, while the markup stays explicit and explainable |
| 0 | 16 decisions (#1–16), all *Proposed* | Reviewed and accepted all of them | Agreed with the reasoning and tradeoffs as written |
| 1 | Phase 0 scaffold: backend solution and projects, `Directory.Build.props`, frontend from `create-vite` 9.3.3 with Tailwind 4, root `.gitignore`. New decisions #17 and #18 as *Proposed* | Ran `dotnet` and `npm` commands myself with no issues. Judged the implementation clean. Renamed `docs/` to `.docs/` and asked for all references to be updated | Wanted the docs folder listed first |
