import type { components } from '../api/schema'
import { buildMonthGrid, formatLongDate, monthOf, weekdayLabels } from '../dates'
import { formatAmount, formatCompact } from '../format'

type Day = components['schemas']['ProjectionDayResponse']

type Props = {
  month: string
  days: Map<string, Day>
  selected: string | null
  onSelect: (iso: string) => void
}

export default function CalendarGrid({ month, days, selected, onSelect }: Props) {
  const cells = buildMonthGrid(month)

  return (
    <div>
      <div className="grid grid-cols-7 gap-1 text-center text-xs font-medium text-slate-500">
        {weekdayLabels.map((label) => (
          <div key={label} className="py-1">
            {label}
          </div>
        ))}
      </div>

      <div className="mt-1 grid grid-cols-7 gap-1">
        {cells.map((iso) => {
          if (monthOf(iso) !== month) return <div key={iso} />

          const dayNumber = Number(iso.slice(8))
          const day = days.get(iso)

          // A day outside the projected range: shown, but not clickable.
          if (!day) {
            return (
              <div key={iso} className="min-h-16 rounded-md bg-slate-100 p-1 text-xs text-slate-600 sm:min-h-24">
                {dayNumber}
              </div>
            )
          }

          const balance = Number(day.closingBalance)
          const isSelected = selected === iso
          const entryCount = day.entries.length
          const tone = day.isWarning
            ? 'border-red-300 bg-red-50'
            : 'border-slate-200 bg-white hover:border-slate-400'

          // What a screen reader says. The visible layer below is hidden from assistive technology,
          // so the spoken name is always complete and the two can never disagree.
          const spokenName =
            `${formatLongDate(iso)}. Projected balance ${formatAmount(balance)}.` +
            (day.isWarning ? ' Warning: below zero.' : '') +
            (entryCount > 0 ? ` ${entryCount} ${entryCount === 1 ? 'entry' : 'entries'}.` : '')

          return (
            <button
              key={iso}
              type="button"
              onClick={() => onSelect(iso)}
              aria-pressed={isSelected}
              className={`relative flex min-h-16 flex-col rounded-md border p-1 text-left text-xs sm:min-h-24 ${tone} ${
                isSelected ? 'ring-2 ring-slate-900' : ''
              }`}
            >
              <span className="sr-only">{spokenName}</span>

              <span aria-hidden="true" className="flex flex-1 flex-col">
                <span className="flex items-center justify-between">
                  <span className="font-medium text-slate-700">{dayNumber}</span>
                  {day.isWarning && <span className="text-red-700">⚠</span>}
                </span>

                <span className="mt-1 space-y-0.5">
                  {day.entries.slice(0, 2).map((e) => (
                    <span
                      key={e.id}
                      className={`hidden truncate rounded px-1 sm:block ${
                        e.type === 'Income' ? 'bg-green-100 text-green-800' : 'bg-amber-100 text-amber-900'
                      }`}
                    >
                      {e.description}
                    </span>
                  ))}
                  {day.entries.length > 2 && (
                    <span className="hidden text-[10px] text-slate-600 sm:block">
                      +{day.entries.length - 2} more
                    </span>
                  )}
                  <span className="flex gap-0.5 sm:hidden">
                    {day.entries.map((e) => (
                      <span
                        key={e.id}
                        className={`h-1.5 w-1.5 rounded-full ${e.type === 'Income' ? 'bg-green-600' : 'bg-amber-500'}`}
                      />
                    ))}
                  </span>
                </span>

                <span
                  className={`mt-auto text-[10px] sm:text-xs ${
                    balance < 0 ? 'font-semibold text-red-700' : 'text-slate-700'
                  }`}
                >
                  {formatCompact(balance)}
                </span>
              </span>
            </button>
          )
        })}
      </div>
    </div>
  )
}