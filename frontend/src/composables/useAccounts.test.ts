import { describe, expect, it, vi } from 'vitest'
import type { AccountsApi } from '../api/accountsApi'
import { ApiError } from '../api/atmClient'
import type { AccountDto, TransactionDto } from '../types/api'
import { useAccounts } from './useAccounts'

const CHECKING_ID = 'a-1'

/**
 * A small in-memory stand-in for the API. Operations change its state the same way the real backend
 * would, so the tests check the composable's behaviour rather than the mocks.
 */
function fakeBank(startingBalance: number) {
  let balance = startingBalance
  const ledger: TransactionDto[] = []

  const account = (): AccountDto => ({ id: CHECKING_ID, name: 'Checking', number: '****1234', balance })

  const api: AccountsApi = {
    list: vi.fn(async () => [account()]),
    transactions: vi.fn(async () => ({ items: [...ledger], page: 1, pageSize: 20, totalCount: ledger.length })),
    deposit: vi.fn(async (_id, body) => {
      balance += body.amount
      ledger.unshift(entry('Deposit', body.amount, balance))
      return account()
    }),
    withdraw: vi.fn(async () => {
      throw new ApiError({ status: 422, title: 'The request cannot be processed', detail: 'Insufficient funds.' })
    }),
    transfer: vi.fn(),
  }

  return { api, balance: () => balance, ledger }
}

function entry(type: TransactionDto['type'], amount: number, balanceAfter: number): TransactionDto {
  return {
    id: crypto.randomUUID(),
    accountId: CHECKING_ID,
    type,
    amount,
    balanceAfter,
    occurredAt: new Date().toISOString(),
    transferId: null,
    description: null,
  }
}

describe('useAccounts', () => {
  it('refreshes the balance and the history after a successful deposit', async () => {
    const bank = fakeBank(1000)
    const state = useAccounts(bank.api)
    await state.load()
    expect(state.selected.value?.balance).toBe(1000)
    expect(state.history.value).toHaveLength(0)

    const succeeded = await state.deposit(100)

    expect(succeeded).toBe(true)
    expect(state.selected.value?.balance).toBe(1100)
    expect(state.history.value).toHaveLength(1)
    expect(state.history.value[0]).toMatchObject({ type: 'Deposit', amount: 100, balanceAfter: 1100 })
    expect(state.error.value).toBeNull()
  })

  it('sets the error and leaves balances and history unchanged when an operation fails', async () => {
    const bank = fakeBank(1000)
    const state = useAccounts(bank.api)
    await state.load()
    const historyBefore = [...state.history.value]

    const succeeded = await state.withdraw(5000)

    expect(succeeded).toBe(false)
    expect(state.error.value).toBeInstanceOf(ApiError)
    expect(state.error.value?.detail).toBe('Insufficient funds.')
    expect(state.selected.value?.balance).toBe(1000)
    expect(state.history.value).toEqual(historyBefore)
    expect(bank.balance()).toBe(1000)
  })

  it('sets pending while an operation runs and clears it afterwards', async () => {
    const bank = fakeBank(1000)
    let finishDeposit: () => void = () => undefined
    bank.api.deposit = vi.fn(
      () =>
        new Promise<AccountDto>((resolve) => {
          finishDeposit = () => resolve({ id: CHECKING_ID, name: 'Checking', number: '****1234', balance: 1050 })
        }),
    )
    const state = useAccounts(bank.api)
    await state.load()

    const operation = state.deposit(50)
    expect(state.pending.value).toBe(true)

    finishDeposit()
    await operation

    expect(state.pending.value).toBe(false)
  })
})
