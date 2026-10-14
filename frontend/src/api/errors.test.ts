import { describe, expect, it } from 'vitest'
import { ApiError, fieldErrors, generalMessage, toApiError } from './errors'

describe('toApiError', () => {
  it('prefers the detail text', () => {
    expect(toApiError({ title: 'Bad request', detail: 'Header missing' }).message).toBe('Header missing')
  })

  it('falls back to the title, then to a generic message', () => {
    expect(toApiError({ title: 'Not Found' }).message).toBe('Not Found')
    expect(toApiError(undefined).message).toBe('Request failed')
  })

  it('keeps per-field errors', () => {
    const error = toApiError({ title: 'Invalid', errors: { Name: ["'Name' must not be empty."] } })
    expect(error.errors.Name).toEqual(["'Name' must not be empty."])
  })

  it('uses a general (empty-key) error as the message', () => {
    const error = toApiError({
      title: 'Invalid',
      errors: { '': ['You can have at most 50 scenarios.'] },
    })
    expect(error.message).toBe('You can have at most 50 scenarios.')
  })
})

describe('fieldErrors', () => {
  const error = new ApiError('Invalid', { Name: ['Required.'], StartingBalance: ['Too large.'] })

  it('matches field names case-insensitively', () => {
    expect(fieldErrors(error, 'name')).toEqual(['Required.'])
    expect(fieldErrors(error, 'startingBalance')).toEqual(['Too large.'])
  })

  it('returns nothing for other fields or other error types', () => {
    expect(fieldErrors(error, 'date')).toEqual([])
    expect(fieldErrors(new Error('x'), 'name')).toEqual([])
    expect(fieldErrors(null, 'name')).toEqual([])
  })
})

describe('generalMessage', () => {
  it('returns nothing when there is no error', () => {
    expect(generalMessage(null)).toBeNull()
  })

  it('shows the message of errors that are not tied to a field', () => {
    expect(generalMessage(new ApiError('Could not save'))).toBe('Could not save')
    expect(generalMessage(new Error('Network down'))).toBe('Network down')
  })

  it('shows a general (empty-key) validation error', () => {
    expect(generalMessage(new ApiError('x', { '': ['Limit reached.'] }))).toBe('Limit reached.')
  })

  it('stays quiet when only field errors exist, so they show under their fields', () => {
    expect(generalMessage(new ApiError('Invalid', { Name: ['Required.'] }))).toBeNull()
  })
})