import { describe, expect, it } from 'vitest'
import {
  WEEK_START,
  buildMonthGrid,
  formatLongDate,
  monthLabel,
  monthOf,
  parseIso,
  shiftMonth,
  toIso,
} from './dates'

const inMonth = (cells: string[], month: string) => cells.filter((c) => monthOf(c) === month)

describe('date helpers', () => {
  it('round-trips an ISO date without shifting the day', () => {
    expect(toIso(parseIso('2026-11-01'))).toBe('2026-11-01')
    expect(toIso(parseIso('2028-02-29'))).toBe('2028-02-29')
    expect(toIso(parseIso('2026-12-31'))).toBe('2026-12-31')
  })

  it('extracts the month from a date', () => {
    expect(monthOf('2026-11-15')).toBe('2026-11')
  })

  it('moves between months across year boundaries', () => {
    expect(shiftMonth('2026-12', 1)).toBe('2027-01')
    expect(shiftMonth('2026-01', -1)).toBe('2025-12')
    expect(shiftMonth('2026-11', 0)).toBe('2026-11')
    expect(shiftMonth('2026-11', 14)).toBe('2028-01')
  })

  it('formats month and day labels', () => {
    expect(monthLabel('2026-11')).toBe('November 2026')
    expect(formatLongDate('2026-11-01')).toBe('Sunday, November 1, 2026')
  })
})

describe('buildMonthGrid', () => {
  it.each([
    ['2026-11', 30],
    ['2026-10', 31],
    ['2028-02', 29], // leap year
    ['2027-02', 28],
  ])('builds whole weeks for %s with %i days', (month, daysInMonth) => {
    const cells = buildMonthGrid(month)

    expect(cells.length % 7).toBe(0)
    expect(parseIso(cells[0]).getUTCDay()).toBe(WEEK_START)

    const own = inMonth(cells, month)
    expect(own).toHaveLength(daysInMonth)
    expect(own[0]).toBe(`${month}-01`)
    expect(own.at(-1)).toBe(`${month}-${String(daysInMonth).padStart(2, '0')}`)
  })

  it('lists days in strict date order with no gaps', () => {
    const cells = buildMonthGrid('2026-11')
    const sorted = [...cells].sort()
    expect(cells).toEqual(sorted)
    expect(new Set(cells).size).toBe(cells.length)
  })
})