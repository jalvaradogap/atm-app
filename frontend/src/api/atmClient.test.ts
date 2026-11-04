import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { AccountDto } from '../types/api'
import { ApiError, request } from './atmClient'

const checking: AccountDto = { id: 'a-1', name: 'Checking', number: '****1234', balance: 1000 }

function jsonResponse(body: unknown, status: number, contentType: string): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

describe('atmClient', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('returns the typed body on success', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([checking], 200, 'application/json'))

    const accounts = await request<AccountDto[]>('GET', '/accounts')

    expect(accounts).toEqual([checking])
    expect(fetchMock).toHaveBeenCalledWith('/api/accounts', expect.objectContaining({ method: 'GET' }))
  })

  it('turns a 422 problem+json into an ApiError with the status and detail', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse(
        {
          title: 'The request cannot be processed',
          status: 422,
          detail: 'Insufficient funds: balance is 1000.00, requested 5000.00.',
        },
        422,
        'application/problem+json',
      ),
    )

    const error = await request('POST', '/accounts/a-1/withdrawals', { amount: 5000 }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(422)
    expect((error as ApiError).detail).toBe('Insufficient funds: balance is 1000.00, requested 5000.00.')
    expect((error as ApiError).unreachable).toBe(false)
  })

  it('uses the first field message when a validation problem has no detail', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { Amount: ['Amount must be between 0.01 and 1,000,000.'] },
        },
        400,
        'application/problem+json',
      ),
    )

    const error = (await request('POST', '/accounts/a-1/deposits', { amount: 0 }).catch((e: unknown) => e)) as ApiError

    expect(error.status).toBe(400)
    expect(error.detail).toBe('Amount must be between 0.01 and 1,000,000.')
  })

  it('turns a network failure into a "cannot reach the server" error', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))

    const error = (await request('GET', '/accounts').catch((e: unknown) => e)) as ApiError

    expect(error).toBeInstanceOf(ApiError)
    expect(error.title).toBe('Cannot reach the server')
    expect(error.unreachable).toBe(true)
    expect(error.status).toBe(0)
  })

  it('turns a non-JSON 500 into a generic error that does not leak the body', async () => {
    fetchMock.mockResolvedValueOnce(new Response('<html>Stack trace: NullReferenceException</html>', { status: 500 }))

    const error = (await request('GET', '/accounts').catch((e: unknown) => e)) as ApiError

    expect(error.status).toBe(500)
    expect(error.title).toBe('Unexpected error')
    expect(error.detail).toBe('Something went wrong. Please try again later.')
    expect(error.detail).not.toContain('NullReferenceException')
  })
})
