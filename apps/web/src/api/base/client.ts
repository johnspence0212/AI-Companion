import { resolveApiBaseUrl } from '@/lib/apiBaseUrl'

export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

const BASE_URL = resolveApiBaseUrl()
const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])
let csrfToken: string | null = null
let csrfRequest: Promise<string> | null = null

function jsonHeaders(hasBody: boolean): Headers {
  const headers = new Headers({ Accept: 'application/json' })
  if (hasBody) {
    headers.set('Content-Type', 'application/json')
  }
  return headers
}

async function readErrorMessage(response: Response): Promise<string> {
  const text = await response.text()
  if (!text) {
    return response.statusText
  }

  try {
    const json = JSON.parse(text) as {
      title?: string
      detail?: string
      message?: string
    }
    return json.detail ?? json.title ?? json.message ?? text
  } catch {
    return text
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const message = await readErrorMessage(response)
    throw new ApiError(message, response.status)
  }
  if (response.status === 204) {
    return undefined as T
  }
  return response.json() as Promise<T>
}

async function fetchCsrfToken(): Promise<string> {
  const response = await fetch(`${BASE_URL}/auth/csrf`, {
    method: 'GET',
    credentials: 'include',
    headers: jsonHeaders(false),
  })
  const payload = await handleResponse<unknown>(response)
  if (
    typeof payload !== 'object' ||
    payload === null ||
    !('requestToken' in payload) ||
    typeof payload.requestToken !== 'string'
  ) {
    throw new ApiError('The server returned an invalid antiforgery token')
  }
  return payload.requestToken
}

async function getCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken
  csrfRequest ??= fetchCsrfToken()
  try {
    csrfToken = await csrfRequest
    return csrfToken
  } finally {
    csrfRequest = null
  }
}

async function request<T>(method: string, url: string, data?: unknown): Promise<T> {
  const hasBody = data !== undefined
  const headers = jsonHeaders(hasBody)
  if (UNSAFE_METHODS.has(method)) {
    headers.set('X-CSRF-TOKEN', await getCsrfToken())
  }

  const response = await fetch(`${BASE_URL}${url}`, {
    method,
    credentials: 'include',
    headers,
    body: hasBody ? JSON.stringify(data) : undefined,
  })
  return handleResponse<T>(response)
}

export function clearCsrfToken(): void {
  csrfToken = null
  csrfRequest = null
}

export const httpClient = {
  get: <T>(url: string): Promise<T> => request<T>('GET', url),

  post: <T, B = unknown>(url: string, data?: B): Promise<T> => request<T>('POST', url, data),

  put: <T = void, B = unknown>(url: string, data: B): Promise<T> => request<T>('PUT', url, data),

  patch: <T = void, B = unknown>(url: string, data: B): Promise<T> =>
    request<T>('PATCH', url, data),

  delete: (url: string): Promise<void> => request<void>('DELETE', url),
}
