export class ApiError extends Error {
  errors: Record<string, string[]>

  constructor(message: string, errors: Record<string, string[]> = {}) {
    super(message)
    this.errors = errors
  }
}

export function toApiError(error: unknown): ApiError {
  const e = error as
    | { title?: string; detail?: string; errors?: Record<string, string[]> }
    | undefined
  return new ApiError(e?.detail ?? e?.title ?? 'Request failed', e?.errors)
}

export function fieldErrors(error: unknown, field: string): string[] {
  if (!(error instanceof ApiError)) return []
  const key = Object.keys(error.errors).find((k) => k.toLowerCase() === field.toLowerCase())
  return key ? error.errors[key] : []
}