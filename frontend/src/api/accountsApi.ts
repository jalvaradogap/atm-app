import type {
  AccountDto,
  AmountRequest,
  PagedResult,
  TransactionDto,
  TransferRequest,
  TransferResultDto,
} from '../types/api'
import { request } from './atmClient'

/** The endpoints the screen uses, one function each (PLAN.md §4). Components use this, never fetch. */
export const accountsApi = {
  list: () => request<AccountDto[]>('GET', '/accounts'),

  transactions: (id: string, page = 1, pageSize = 20) =>
    request<PagedResult<TransactionDto>>('GET', `/accounts/${id}/transactions?page=${page}&pageSize=${pageSize}`),

  deposit: (id: string, body: AmountRequest) =>
    request<AccountDto>('POST', `/accounts/${id}/deposits`, body),

  withdraw: (id: string, body: AmountRequest) =>
    request<AccountDto>('POST', `/accounts/${id}/withdrawals`, body),

  transfer: (body: TransferRequest) =>
    request<TransferResultDto>('POST', '/transfers', body),
}

export type AccountsApi = typeof accountsApi
