import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { components } from '../api/schema'
import { weekdayLabels } from '../dates'
import CalendarGrid from './CalendarGrid'

type Day = components['schemas']['ProjectionDayResponse']

function makeDay(date: string, closingBalance: number): Day {
  return { date, closingBalance, isWarning: closingBalance < 0, entries: [] }
}

const days = new Map<string, Day>([
  ['2026-11-01', makeDay('2026-11-01', -3000)],
  ['2026-11-02', makeDay('2026-11-02', -3000)],
  ['2026-11-05', makeDay('2026-11-05', 3000)],
])

describe('CalendarGrid', () => {
  it('shows a button only for days inside the projection', () => {
    render(<CalendarGrid month="2026-11" days={days} selected={null} onSelect={() => {}} />)

    expect(screen.getAllByRole('button')).toHaveLength(3)
  })

  it('shows the weekday headings', () => {
    render(<CalendarGrid month="2026-11" days={days} selected={null} onSelect={() => {}} />)

    weekdayLabels.forEach((label) => expect(screen.getByText(label)).toBeInTheDocument())
  })

  it('announces warning days in words, not only by color', () => {
    render(<CalendarGrid month="2026-11" days={days} selected={null} onSelect={() => {}} />)

    const warning = screen.getByRole('button', { name: /November 1, 2026/ })
    const normal = screen.getByRole('button', { name: /November 5, 2026/ })

    expect(warning).toHaveAccessibleName(/Projected balance -3,000\.00\. Warning: below zero\./)
    expect(normal).toHaveAccessibleName(/Projected balance 3,000\.00\./)
    expect(normal.getAttribute('aria-label')).not.toContain('Warning')
  })

  it('reports which day was clicked', async () => {
    const onSelect = vi.fn()
    render(<CalendarGrid month="2026-11" days={days} selected={null} onSelect={onSelect} />)

    await userEvent.click(screen.getByRole('button', { name: /November 5, 2026/ }))

    expect(onSelect).toHaveBeenCalledWith('2026-11-05')
  })

  it('marks the selected day as pressed', () => {
    render(<CalendarGrid month="2026-11" days={days} selected="2026-11-02" onSelect={() => {}} />)

    expect(screen.getByRole('button', { name: /November 2, 2026/ })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: /November 1, 2026/ })).toHaveAttribute('aria-pressed', 'false')
  })
})