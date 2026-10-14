import type { components } from '../api/schema'
import { formatLongDate } from '../dates'
import { formatAmount } from '../format'

type Day = components['schemas']['ProjectionDayResponse']

type Props = {
  date: string
  days: Day[]
  startingBalance: number | string
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex justify-between py-1 ${strong ? 'border-t border-slate-200 font-semibold' : ''}`}>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

export default function DayDetail({ date, days, startingBalance }: Props) {
  const index = days.findIndex((d) => d.date === date)
  if (index === -1) return null

  const day = days[index]
  const opening = index === 0 ? Number(startingBalance) : Number(days[index - 1].closingBalance)
  const closing = Number(day.closingBalance)
  const income = day.entries
    .filter((e) => e.type === 'Income')
    .reduce((sum, e) => sum + Number(e.amount), 0)
  const bills = day.entries
    .filter((e) => e.type === 'Bill')
    .reduce((sum, e) => sum + Number(e.amount), 0)

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h4 className="font-semibold">{formatLongDate(date)}</h4>

      {day.isWarning && (
        <p className="mt-2 rounded-md bg-red-50 p-2 text-sm text-red-900">
          <span aria-hidden="true">⚠ </span>
          Projected balance: {formatAmount(closing)} on this day.
        </p>
      )}

      <dl className="mt-3 text-sm">
        <Row label="Opening balance" value={formatAmount(opening)} />
        <Row label="+ Income" value={formatAmount(income)} />
        <Row label="- Bills" value={formatAmount(bills)} />
        <Row label="Closing balance" value={formatAmount(closing)} strong />
      </dl>

      {day.entries.length === 0 ? (
        <p className="mt-3 text-sm text-slate-500">No entries on this day.</p>
      ) : (
        <ul className="mt-3 space-y-1 text-sm">
          {day.entries.map((e) => (
            <li key={e.id} className="flex justify-between">
              <span>
                {e.description} <span className="text-slate-500">({e.category})</span>
              </span>
              <span className={e.type === 'Income' ? 'text-green-700' : 'text-red-700'}>
                {e.type === 'Income' ? '+' : '-'}
                {formatAmount(e.amount)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}