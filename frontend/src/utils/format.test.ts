import { describe, expect, it } from 'vitest'
import type { AccountDto, TransactionDto } from '../types/api'
import { formatMoney, formatSignedMoney, transactionLabel } from './format'

const accounts: AccountDto[] = [
  { id: 'checking-id', name: 'Checking', number: '****1234', balance: 0 },
  { id: 'savings-id', name: 'Savings', number: '****5678', balance: 0 },
]

function entry(overrides: Partial<TransactionDto>): TransactionDto {
  return {
    id: 'tx-1',
    accountId: 'checking-id',
    type: 'Deposit',
    amount: 200,
    balanceAfter: 1200,
    occurredAt: '2026-11-03T16:42:00Z',
    transferId: null,
    description: null,
    ...overrides,
  }
}

describe('formatMoney', () => {
  it('formats USD with grouping and two decimals', () => {
    expect(formatMoney(1200)).toBe('$1,200.00')
    expect(formatMoney(0.1)).toBe('$0.10')
  })
})

describe('formatSignedMoney', () => {
  it('adds a plus sign to credits and a minus sign to debits', () => {
    expect(formatSignedMoney('Deposit', 200)).toBe('+$200.00')
    expect(formatSignedMoney('TransferIn', 50)).toBe('+$50.00')
    expect(formatSignedMoney('Withdrawal', 50)).toBe('−$50.00')
    expect(formatSignedMoney('TransferOut', 50)).toBe('−$50.00')
  })
})

describe('transactionLabel', () => {
  it('names the other account in a transfer', () => {
    expect(transactionLabel(entry({ type: 'TransferOut', accountId: 'checking-id' }), accounts)).toBe('Transfer to Savings')
    expect(transactionLabel(entry({ type: 'TransferIn', accountId: 'savings-id' }), accounts)).toBe('Transfer from Checking')
  })

  it('uses a plain label for deposits and withdrawals', () => {
    expect(transactionLabel(entry({ type: 'Deposit' }), accounts)).toBe('Deposit')
    expect(transactionLabel(entry({ type: 'Withdrawal' }), accounts)).toBe('Withdrawal')
  })
})
