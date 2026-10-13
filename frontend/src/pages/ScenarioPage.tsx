import { Link, useParams } from 'react-router'
import { useScenario } from '../api/hooks'
import { formatAmount } from '../format'

export default function ScenarioPage() {
  const { id } = useParams()
  const scenario = useScenario(Number(id))

  return (
    <div className="space-y-4">
      <Link to="/" className="text-sm text-slate-600 hover:underline">
        &larr; All scenarios
      </Link>

      {scenario.isPending && <p className="text-sm text-slate-500">Loading...</p>}
      {scenario.isError && (
        <p className="text-sm text-red-700">Could not load this scenario: {scenario.error.message}</p>
      )}

      {scenario.data && (
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h2 className="text-xl font-semibold">{scenario.data.name}</h2>
          <p className="mt-1 text-sm text-slate-600">
            Starts {scenario.data.startDate} · starting balance {formatAmount(scenario.data.startingBalance)} ·{' '}
            {scenario.data.entryCount} entries
          </p>
          <p className="mt-4 text-sm text-slate-500">The calendar and projection will appear here next.</p>
        </div>
      )}
    </div>
  )
}