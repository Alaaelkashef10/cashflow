import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getOwnerId } from './ownerId'

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

describe('getOwnerId', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    localStorage.clear()
  })

  it('creates a UUID and remembers it', () => {
    const id = getOwnerId()

    expect(id).toMatch(UUID)
    expect(localStorage.getItem('cashflow.ownerId')).toBe(id)
    expect(getOwnerId()).toBe(id)
  })

  it('reuses an id that is already stored', () => {
    localStorage.setItem('cashflow.ownerId', 'existing-id')

    expect(getOwnerId()).toBe('existing-id')
  })

  it('still gives a stable id when the browser blocks storage', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    const first = getOwnerId()

    expect(first).toMatch(UUID)
    expect(getOwnerId()).toBe(first)
  })
})