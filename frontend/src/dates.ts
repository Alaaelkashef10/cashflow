const MS_PER_DAY = 86_400_000

// 0 = Sunday, 1 = Monday, 6 = Saturday. Change this to match your calendar preference.
export const WEEK_START = 0

const WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
export const weekdayLabels = Array.from({ length: 7 }, (_, i) => WEEKDAYS[(WEEK_START + i) % 7])

// Dates travel as 'YYYY-MM-DD' strings. All math below uses UTC, so time zones never shift a day.
export function parseIso(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(Date.UTC(y, m - 1, d))
}

export function toIso(date: Date): string {
  return date.toISOString().slice(0, 10)
}

export const monthOf = (iso: string) => iso.slice(0, 7)

export function monthLabel(month: string): string {
  const [y, m] = month.split('-').map(Number)
  return new Date(Date.UTC(y, m - 1, 1)).toLocaleDateString('en-US', {
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  })
}

export function shiftMonth(month: string, delta: number): string {
  const [y, m] = month.split('-').map(Number)
  return toIso(new Date(Date.UTC(y, m - 1 + delta, 1))).slice(0, 7)
}

export function formatLongDate(iso: string): string {
  return parseIso(iso).toLocaleDateString('en-US', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  })
}

// Every date shown in a month grid, padded with neighbouring days to whole weeks.
export function buildMonthGrid(month: string): string[] {
  const [y, m] = month.split('-').map(Number)
  const first = Date.UTC(y, m - 1, 1)
  const daysInMonth = new Date(Date.UTC(y, m, 0)).getUTCDate()
  const offset = (new Date(first).getUTCDay() - WEEK_START + 7) % 7
  const cells = Math.ceil((offset + daysInMonth) / 7) * 7
  return Array.from({ length: cells }, (_, i) => toIso(new Date(first + (i - offset) * MS_PER_DAY)))
}