// Types that mirror the backend DTOs (backend/src/Atm.Application/Dtos).
// Keep them in step with the API when the contract changes.

export interface AccountDto {
  id: string
  name: string
  number: string
  balance: number
}

export type TransactionType = 'Deposit' | 'Withdrawal' | 'TransferIn' | 'TransferOut'

export interface TransactionDto {
  id: string
  accountId: string
  type: TransactionType
  amount: number
  balanceAfter: number
  occurredAt: string
  transferId: string | null
  description: string | null
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface TransferResultDto {
  transferId: string
  from: AccountDto
  to: AccountDto
}

/** Request body for deposits and withdrawals. */
export interface AmountRequest {
  amount: number
  description?: string
}

/** Request body for transfers. */
export interface TransferRequest {
  fromAccountId: string
  toAccountId: string
  amount: number
  description?: string
}

/** RFC 7807 problem details, as returned by the API for every error. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
