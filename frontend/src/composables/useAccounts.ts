import { computed, ref } from 'vue'
import { accountsApi as defaultApi, type AccountsApi } from '../api/accountsApi'
import { ApiError } from '../api/atmClient'
import type { AccountDto, TransactionDto } from '../types/api'

/** The UX shows the latest 20 entries, with no "Load more" (decision #15). */
export const HISTORY_PAGE_SIZE = 20

/**
 * State and actions for the ATM screen. The API is a parameter so tests can pass fakes.
 * Every operation refreshes the balances and the history after it succeeds. A failed operation
 * sets `error` and changes nothing else.
 */
export function useAccounts(api: AccountsApi = defaultApi) {
  const accounts = ref<AccountDto[]>([])
  const selectedId = ref<string | null>(null)
  const history = ref<TransactionDto[]>([])
  const pending = ref(false)
  const error = ref<ApiError | null>(null)

  const selected = computed(() => accounts.value.find((a) => a.id === selectedId.value) ?? null)

  async function loadAccounts(): Promise<void> {
    accounts.value = await api.list()
    const stillExists = accounts.value.some((a) => a.id === selectedId.value)
    if (!stillExists) {
      selectedId.value = accounts.value[0]?.id ?? null
    }
  }

  async function loadHistory(): Promise<void> {
    if (selectedId.value === null) {
      history.value = []
      return
    }
    const page = await api.transactions(selectedId.value, 1, HISTORY_PAGE_SIZE)
    history.value = page.items
  }

  async function refresh(): Promise<void> {
    await loadAccounts()
    await loadHistory()
  }

  /** Runs one operation with the pending and error state. Returns true when it succeeded. */
  async function attempt(work: () => Promise<void>): Promise<boolean> {
    pending.value = true
    error.value = null
    try {
      await work()
      return true
    } catch (caught) {
      error.value = toApiError(caught)
      return false
    } finally {
      pending.value = false
    }
  }

  function load(): Promise<boolean> {
    return attempt(refresh)
  }

  function select(id: string): Promise<boolean> {
    selectedId.value = id
    return attempt(loadHistory)
  }

  function deposit(amount: number, description?: string): Promise<boolean> {
    return attempt(async () => {
      await api.deposit(requireSelected(), { amount, description })
      await refresh()
    })
  }

  function withdraw(amount: number, description?: string): Promise<boolean> {
    return attempt(async () => {
      await api.withdraw(requireSelected(), { amount, description })
      await refresh()
    })
  }

  function transfer(toAccountId: string, amount: number, description?: string): Promise<boolean> {
    return attempt(async () => {
      await api.transfer({ fromAccountId: requireSelected(), toAccountId, amount, description })
      await refresh()
    })
  }

  function requireSelected(): string {
    if (selectedId.value === null) {
      throw new ApiError({ status: 0, title: 'No account selected' })
    }
    return selectedId.value
  }

  return {
    accounts,
    selected,
    selectedId,
    history,
    pending,
    error,
    load,
    select,
    deposit,
    withdraw,
    transfer,
  }
}

function toApiError(caught: unknown): ApiError {
  if (caught instanceof ApiError) {
    return caught
  }
  return new ApiError({ status: 500, title: 'Unexpected error', detail: 'Something went wrong. Please try again later.' })
}
