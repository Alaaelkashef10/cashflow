import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { components } from '../api/schema'
import ProjectionSummary from './ProjectionSummary'

type Projection = components['schemas']['ProjectionResponse']

function makeProjection(overrides: Partial<Projection> = {}): Projection {
  return {
    scenarioId: 1,
    startingBalance: 1000,
    startDate: '2026-11-01',
    endDate: '2026-11-10',
    days: [],
    warningDates: [],
    summary: {
      lowestBalance: 700,
      lowestBalanceDate: '2026-11-03',
      warningDayCount: 0,
      endingBalance: 7700,
    },
    ...overrides,
  }
}

describe('ProjectionSummary', () => {
  it('raises an alert when the balance goes below zero', () => {
    render(
      <ProjectionSummary
        projection={makeProjection({
          warningDates: ['2026-11-01', '2026-11-02', '2026-11-03', '2026-11-04'],
          summary: {
            lowestBalance: -3000,
            lowestBalanceDate: '2026-11-01',
            warningDayCount: 4,
            endingBalance: 2100,
          },
        })}
      />,
    )

    const alert = screen.getByRole('alert')
    expect(alert).toHaveTextContent(/below zero on 4 days/)
    expect(alert).toHaveTextContent('Sunday, November 1, 2026')
    expect(screen.getByText('-3,000.00')).toBeInTheDocument()
    expect(screen.getByText('2,100.00')).toBeInTheDocument()
  })

  it('uses the singular when only one day is affected', () => {
    render(
      <ProjectionSummary
        projection={makeProjection({
          warningDates: ['2026-11-01'],
          summary: { lowestBalance: -10, lowestBalanceDate: '2026-11-01', warningDayCount: 1, endingBalance: 50 },
        })}
      />,
    )

    expect(screen.getByRole('alert')).toHaveTextContent(/below zero on 1 day,/)
  })

  it('shows a calm message and no alert when nothing goes below zero', () => {
    render(<ProjectionSummary projection={makeProjection()} />)

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.getByText('No days below zero in this projection.')).toBeInTheDocument()
    expect(screen.getByText('700.00')).toBeInTheDocument()
    expect(screen.getByText('7,700.00')).toBeInTheDocument()
  })
})