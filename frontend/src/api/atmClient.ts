import type { ProblemDetails } from '../types/api'

const API_BASE = '/api'

/** Every failed call becomes one of these, so the UI handles a single error type. */
export class ApiError extends Error {
  readonly title: string
  readonly status: number
  readonly detail: string | undefined
  /** True when the server could not be reached at all (the banner with Retry). */
  readonly unreachable: boolean

  constructor(options: { status: number; title: string; detail?: string; unreachable?: boolean }) {
    super(options.title)
    this.name = 'ApiError'
    this.title = options.title
    this.status = options.status
    this.detail = options.detail
    this.unreachable = options.unreachable ?? false
  }
}

/**
 * The only function that calls fetch. Success returns the parsed body. Failures throw ApiError.
 */
export async function request<T>(method: 'GET' | 'POST', path: string, body?: unknown): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    // fetch rejects only when no response arrives: the server is down or the network failed.
    throw new ApiError({
      status: 0,
      title: 'Cannot reach the server',
      detail: 'Check that the API is running, then try again.',
      unreachable: true,
    })
  }

  if (response.ok) {
    return (await response.json()) as T
  }

  throw await toApiError(response)
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = await readProblem(response)

  if (problem) {
    // Validation failures carry field messages in `errors` rather than in `detail`.
    const firstFieldMessage = problem.errors ? Object.values(problem.errors).flat()[0] : undefined
    return new ApiError({
      status: response.status,
      title: problem.title ?? 'Request failed',
      detail: problem.detail ?? firstFieldMessage,
    })
  }

  // A body that isn't problem JSON (for example, a proxy's HTML error page) never reaches the user.
  return new ApiError({
    status: response.status,
    title: 'Unexpected error',
    detail: 'Something went wrong. Please try again later.',
  })
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    const body: unknown = await response.json()
    return typeof body === 'object' && body !== null ? (body as ProblemDetails) : null
  } catch {
    return null
  }
}
