import { useMemo, useState } from 'react'
import { useProjection } from '../api/hooks'
import { fieldErrors, generalMessage } from '../api/errors'
import type { components } from '../api/schema'
import { monthLabel, monthOf, shiftMonth } from '../dates'
import CalendarGrid from './CalendarGrid'
import DayDetail from './DayDetail'
import ProjectionSummary from './ProjectionSummary'

type Day = components['schemas']['ProjectionDayResponse']

type Props = {
  scenarioId: number
  startDate: string
}

function clampMonth(month: string, min: string, max: string) {
  if (month < min) return min
  if (month > max) return max
  return month
}

export default function ProjectionSection({ scenarioId, startDate }: Props) {
  const [horizonEnd, setHorizonEnd] = useState('')
  const [month, setMonth] = useState<string | null>(null)
  const [selected, setSelected] = useState<string | null>(null)

  const projection = useProjection(scenarioId, horizonEnd)
  const data = projection.data

  const dayMap = useMemo(
    () => new Map<string, Day>((data?.days ?? []).map((d) => [d.date, d] as const)),
    [data],
  )

  const minMonth = data ? monthOf(data.startDate) : monthOf(startDate)
  const maxMonth = data ? monthOf(data.endDate) : minMonth
  const shownMonth = clampMonth(month ?? minMonth, minMonth, maxMonth)
  const activeDate = selected ?? data?.warningDates[0] ?? null

  const errorMessage = projection.isError
    ? fieldErrors(projection.error, 'horizonEnd').join(' ') ||
      generalMessage(projection.error) ||
      'Could not load the projection.'
    : null

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold">Projected balance</h3>
          <p className="text-sm text-slate-600">
            An estimate based on the entries below. It is not financial advice.
          </p>
        </div>
        <div className="flex items-end gap-2">
          <div>
            <label className="block text-sm font-medium" htmlFor="horizon">
              Project until (optional)
            </label>
            <input
              id="horizon"
              type="date"
              min={startDate}
              value={horizonEnd}
              onChange={(e) => setHorizonEnd(e.target.value)}
              className="mt-1 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm"
            />
          </div>
          {horizonEnd && (
            <button
              type="button"
              onClick={() => setHorizonEnd('')}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm hover:bg-slate-100"
            >
              Reset
            </button>
          )}
        </div>
      </div>

      {projection.isPending && <p className="text-sm text-slate-500">Loading projection...</p>}
      {errorMessage && <p className="text-sm text-red-700">{errorMessage}</p>}

      {data && (
        <>
          <ProjectionSummary projection={data} />

          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
            <div className="mb-3 flex items-center justify-between">
              <button
                type="button"
                onClick={() => setMonth(shiftMonth(shownMonth, -1))}
                disabled={shownMonth <= minMonth}
                className="rounded-md border border-slate-300 bg-white px-3 py-1 text-sm hover:bg-slate-100 disabled:opacity-40"
              >
                &larr; Previous
              </button>
              <h4 className="font-semibold" aria-live="polite">
                {monthLabel(shownMonth)}
              </h4>
              <button
                type="button"
                onClick={() => setMonth(shiftMonth(shownMonth, 1))}
                disabled={shownMonth >= maxMonth}
                className="rounded-md border border-slate-300 bg-white px-3 py-1 text-sm hover:bg-slate-100 disabled:opacity-40"
              >
                Next &rarr;
              </button>
            </div>

            <CalendarGrid month={shownMonth} days={dayMap} selected={activeDate} onSelect={setSelected} />

            <p className="mt-3 text-xs text-slate-500">
              <span className="rounded bg-green-100 px-1 text-green-800">Income</span>{' '}
              <span className="rounded bg-amber-100 px-1 text-amber-900">Bill</span>{' '}
              <span className="text-red-700">⚠ Balance below zero</span> · The number in each day is the closing
              balance.
            </p>
          </div>

          {activeDate ? (
            <DayDetail date={activeDate} days={data.days} startingBalance={data.startingBalance} />
          ) : (
            <p className="text-sm text-slate-500">Select a day to see its details.</p>
          )}
        </>
      )}
    </section>
  )
}