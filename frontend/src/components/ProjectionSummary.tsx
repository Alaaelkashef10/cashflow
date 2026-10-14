import type { components } from '../api/schema'
import { formatLongDate } from '../dates'
import { formatAmount } from '../format'

type Projection = components['schemas']['ProjectionResponse']

function Stat({ label, value, note, negative }: { label: string; value: string; note?: string; negative?: boolean }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3">
      <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</dt>
      <dd className={`mt-1 text-xl font-semibold ${negative ? 'text-red-700' : 'text-slate-900'}`}>{value}</dd>
      {note && <dd className="mt-1 text-xs text-slate-500">{note}</dd>}
    </div>
  )
}

export default function ProjectionSummary({ projection }: { projection: Projection }) {
  const { summary, warningDates } = projection
  const warningCount = Number(summary.warningDayCount)
  const lowest = Number(summary.lowestBalance)
  const ending = Number(summary.endingBalance)

  return (
    <div className="space-y-3">
      {warningCount > 0 ? (
        <div role="alert" className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-900">
          <span aria-hidden="true">⚠ </span>
          <strong>Heads up:</strong> your projected balance is below zero on {warningCount}{' '}
          {warningCount === 1 ? 'day' : 'days'}, starting {formatLongDate(warningDates[0])}.
        </div>
      ) : (
        <div className="rounded-lg border border-green-300 bg-green-50 p-3 text-sm text-green-900">
          No days below zero in this projection.
        </div>
      )}

      <dl className="grid gap-3 sm:grid-cols-3">
        <Stat
          label="Lowest balance"
          value={formatAmount(lowest)}
          note={formatLongDate(summary.lowestBalanceDate)}
          negative={lowest < 0}
        />
        <Stat label="Days below zero" value={String(warningCount)} negative={warningCount > 0} />
        <Stat
          label="Ending balance"
          value={formatAmount(ending)}
          note={formatLongDate(projection.endDate)}
          negative={ending < 0}
        />
      </dl>
    </div>
  )
}