import { describe, expect, it } from 'vitest'
import { formatAmount, formatCompact } from './format'

describe('formatAmount', () => {
  it('uses thousands separators and two decimals', () => {
    expect(formatAmount(1234.5)).toBe('1,234.50')
    expect(formatAmount(0.5)).toBe('0.50')
  })

  it('accepts numbers sent as strings', () => {
    expect(formatAmount('2500')).toBe('2,500.00')
  })

  it('keeps the sign of negative balances', () => {
    expect(formatAmount(-3000)).toBe('-3,000.00')
  })
})

describe('formatCompact', () => {
  it('shortens large numbers', () => {
    expect(formatCompact(1500)).toBe('1.5K')
    expect(formatCompact(-3000)).toBe('-3K')
    expect(formatCompact(999)).toBe('999')
  })
})