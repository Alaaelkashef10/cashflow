import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// React Testing Library only knows how to wait with fake timers when a global `jest` exists.
// Vitest has no such global, so without this shim userEvent hangs whenever fake timers are on.
;(globalThis as unknown as { jest?: unknown }).jest = {
  advanceTimersByTime: (ms: number) => vi.advanceTimersByTime(ms),
}

afterEach(() => cleanup())