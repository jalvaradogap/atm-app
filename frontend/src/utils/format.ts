import type { AccountDto, TransactionDto, TransactionType } from '../types/api'

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const dateTime = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' })

/** "$1,200.00". Intl handles the grouping and the two decimals. */
export function formatMoney(amount: number): string {
  return money.format(amount)
}

/** Money with a sign, for history rows: "+$200.00" for credits, "−$50.00" for debits (U+2212). */
export function formatSignedMoney(type: TransactionType, amount: number): string {
  return isCredit(type) ? `+${formatMoney(amount)}` : `−${formatMoney(amount)}`
}

export function isCredit(type: TransactionType): boolean {
  return type === 'Deposit' || type === 'TransferIn'
}

/** A human label, such as "Transfer to Savings". The other account's name comes from the account list. */
export function transactionLabel(transaction: TransactionDto, accounts: readonly AccountDto[]): string {
  const other = accounts.find((account) => account.id !== transaction.accountId)?.name ?? 'other account'

  switch (transaction.type) {
    case 'Deposit':
      return 'Deposit'
    case 'Withdrawal':
      return 'Withdrawal'
    case 'TransferOut':
      return `Transfer to ${other}`
    case 'TransferIn':
      return `Transfer from ${other}`
  }
}

/** Local date and time. The API sends UTC, and the browser shows it in the user's time zone. */
export function formatDateTime(isoUtc: string): string {
  return dateTime.format(new Date(isoUtc))
}
