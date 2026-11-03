# UX.md: Lightweight UX and design spec

The requirement says UX isn't a primary criterion, "but good design is still good design".
The goal here is an interface that is **clear, prevents mistakes, and always tells the user what
happened**, styled with **Tailwind CSS 4** utility classes in the time already budgeted for
Phase 4b. There's no component library: every element is written by hand. The decisions are
logged as #15 (UX) and #16 (Tailwind) in [PLAN.md](PLAN.md).

## 1. Principles

1. **Prevent errors rather than report them.** Make invalid actions impossible or obviously
   disabled before the user hits the backend.
2. **Always show the outcome.** Every action ends in a visible success or error state that
   includes the new balance.
3. **Money is easy to read.** Currency formatting, aligned digits, a sign *and* a color for
   credits and debits.
4. **Works without a mouse and on a phone.** Real form elements, labels, keyboard focus, and a
   single-column layout on narrow screens.

## 2. Layout

Desktop (≥ 768 px): two columns. On the left are the accounts and the action form. On the
right is the history of the selected account.

```
┌──────────────────────────────────────────────────────────────────────┐
│  ATM                                                                 │
├───────────────────────────────┬──────────────────────────────────────┤
│ ┌───────────┐ ┌───────────┐   │  Checking ****1234 · History         │
│ │▌Checking  │ │ Savings   │   │  ─────────────────────────────────── │
│ │ ****1234  │ │ ****5678  │   │  Nov 3, 10:42  Deposit     +$200.00  │
│ │ $1,200.00 │ │ $5,000.00 │   │                  bal.  $1,200.00     │
│ └───────────┘ └───────────┘   │  Nov 3, 10:40  Transfer to  −$50.00  │
│   (selected)                  │                Savings               │
│ ┌───────────────────────────┐ │                  bal.  $1,000.00     │
│ │ Deposit|Withdraw|Transfer │ │  ...                                 │
│ │ Amount  [ $ ____.__ ]     │ │                                      │
│ │ Available: $1,200.00      │ │                                      │
│ │ [  Deposit $200.00  ]     │ │                                      │
│ │ ✓ Deposited $200.00. New  │ │                                      │
│ │   balance $1,200.00       │ │                                      │
│ └───────────────────────────┘ │                                      │
└───────────────────────────────┴──────────────────────────────────────┘
```

Mobile (< 768 px): everything in one column, in this order: account cards (side by side if they fit), the
form, then the history.

## 3. Interaction design

| Situation | Behaviour |
|-----------|-----------|
| **Selecting an account** | Click a card. The selected card gets an accent border plus a "Selected" label, so color isn't the only cue. The form and history follow the selection. |
| **Choosing an operation** | Three tabs: Deposit, Withdraw, Transfer. Switching tabs keeps the typed amount. |
| **Transfer direction** | There are exactly two accounts, so there's **no destination dropdown**. The form shows `From Checking → To Savings` with a **⇄ swap** button, which makes a same-account transfer impossible by design. |
| **Amount input** | `<input inputmode="decimal">` with a `$` prefix and a label. It's validated as you type with `validateAmount`, and the reason shows below the field ("Maximum 2 decimal places", "Exceeds available balance $1,200.00"). |
| **Context** | "Available: $X" shows under the field for Withdraw and Transfer. |
| **Submit button** | The label repeats the action and amount (`Withdraw $50.00`). It's disabled while the amount is invalid. `Enter` submits, because it's a real `<form>`. |
| **While processing** | The button says "Processing…" and the inputs are disabled, which prevents double submits. |
| **Success** | A green message with the new balance(s), for example "Transferred $50.00. Checking: $1,150.00 · Savings: $5,050.00". The amount is cleared, and the new history entry is briefly highlighted. |
| **Business error (422)** | A red message under the form, using the backend's `detail` (e.g., "Insufficient funds: available $1,200.00"). The input keeps its value so the user can fix it. |
| **Server unreachable / 500** | A banner at the top: "Can't reach the bank right now", with a **Retry** button. No stack traces or raw JSON. |
| **Initial loading** | Card and history placeholders ("Loading…"), so the layout doesn't jump. |
| **Empty history** | "No transactions yet. Make a deposit to get started." |
| **No confirm dialog** | This is deliberate: amounts are shown on the button and in the result, and a modal adds friction. A real ATM would confirm, and this is a known tradeoff. |

## 4. Visual style

- **Tokens in Tailwind's `@theme`** (in `src/style.css`): a neutral background and surface, one
  accent color, plus `success` and `danger`. Use Tailwind's default spacing scale and `rounded-lg`
  / `shadow-sm`. Components use these tokens (`bg-accent`, `text-danger`), never one-off
  arbitrary values like `bg-[#1e40af]`.
- **Typography:** Tailwind's default `font-sans` (system stack). Amounts use `tabular-nums` and
  are right-aligned in the history, so digits line up.
- **Money:** `Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })` everywhere.
  Credits are `+$200.00` in green, debits `−$50.00` in red. The sign is always shown, so color is never
  the only signal.
- **Dates:** `Intl.DateTimeFormat` short date plus time, in the user's local time zone (the API returns UTC).
- **Transaction labels:** human-readable, so "Transfer to Savings" / "Transfer from Checking"
  instead of `TransferOut`.
- **Dark mode:** Tailwind's `dark:` variant (follows `prefers-color-scheme`). It's cheap, but still
  on the cut line.
- **Keeping templates readable:** when the same class list appears more than twice, extract a
  small component (e.g. `BaseButton`). `@apply` is used sparingly.

## 5. Accessibility checklist

- Every input has a `<label>`, and error text is linked with `aria-describedby`.
- Tabs use `role="tablist"` / `role="tab"` / `aria-selected` and can be operated by keyboard.
- Status and error messages sit in an `aria-live="polite"` region, so screen readers announce results.
- `:focus-visible` outlines are never removed.
- Text contrast is at least WCAG AA (4.5:1), checked with the browser devtools.

## 6. Out of scope (next steps)

- Quick-amount buttons ($20 / $50 / $100) and an ATM-style keypad
- Full paging of the history (the UI shows the latest 20; the API already supports paging)
- Undo, receipts and printing, i18n and multiple currencies
- A design system or UI component library
