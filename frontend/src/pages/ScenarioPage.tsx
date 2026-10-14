import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { useDeleteEntry, useEntries, useScenario } from '../api/hooks'
import EntryForm from '../components/EntryForm'
import ProjectionSection from '../components/ProjectionSection'
import { formatAmount } from '../format'

export default function ScenarioPage() {
  const { id } = useParams()
  const scenarioId = Number(id)

  const scenario = useScenario(scenarioId)
  const entries = useEntries(scenarioId)
  const deleteEntry = useDeleteEntry()
  const [editingId, setEditingId] = useState<number | null>(null)

  function handleDelete(entryId: number, description: string) {
    if (window.confirm(`Delete "${description}"?`)) {
      deleteEntry.mutate(entryId)
    }
  }

  return (
    <div className="space-y-6">
      <Link to="/" className="text-sm text-slate-600 hover:underline">
        &larr; All scenarios
      </Link>

      {scenario.isPending && <p className="text-sm text-slate-500">Loading...</p>}
      {scenario.isError && (
        <p className="text-sm text-red-700">Could not load this scenario: {scenario.error.message}</p>
      )}

      {scenario.data && (
        <>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <h2 className="text-xl font-semibold">{scenario.data.name}</h2>
            <p className="mt-1 text-sm text-slate-600">
              Starts {scenario.data.startDate} · starting balance {formatAmount(scenario.data.startingBalance)}
            </p>
          </div>

          <ProjectionSection scenarioId={scenarioId} startDate={scenario.data.startDate} />

          <section>
            <h3 className="text-lg font-semibold">Add an entry</h3>
            <div className="mt-3">
              <EntryForm scenarioId={scenarioId} defaultDate={scenario.data.startDate} />
            </div>
          </section>

          <section>
            <h3 className="text-lg font-semibold">Entries</h3>

            {entries.isPending && <p className="mt-3 text-sm text-slate-500">Loading entries...</p>}
            {entries.isError && (
              <p className="mt-3 text-sm text-red-700">Could not load entries: {entries.error.message}</p>
            )}
            {entries.data?.length === 0 && (
              <p className="mt-3 text-sm text-slate-600">No entries yet. Add your first income or bill above.</p>
            )}

            <ul className="mt-3 space-y-2">
              {entries.data?.map((entry) => {
                const entryId = Number(entry.id)

                if (editingId === entryId) {
                  return (
                    <li key={entry.id}>
                      <EntryForm
                        scenarioId={scenarioId}
                        defaultDate={scenario.data.startDate}
                        entry={entry}
                        onDone={() => setEditingId(null)}
                        onCancel={() => setEditingId(null)}
                      />
                    </li>
                  )
                }

                const isIncome = entry.type === 'Income'
                return (
                  <li
                    key={entry.id}
                    className="flex items-center justify-between gap-3 rounded-lg border border-slate-200 bg-white p-3"
                  >
                    <div className="min-w-0 flex-1">
                      <span className="block truncate font-medium">{entry.description}</span>
                      <span className="block text-sm text-slate-600">
                        {entry.date} · {entry.category}
                      </span>
                    </div>
                    <span className={isIncome ? 'font-medium text-green-700' : 'font-medium text-red-700'}>
                      {isIncome ? '+' : '-'}
                      {formatAmount(entry.amount)}
                    </span>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => setEditingId(entryId)}
                        className="rounded-md border border-slate-300 px-3 py-1 text-sm hover:bg-slate-100"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(entryId, entry.description)}
                        className="rounded-md border border-slate-300 px-3 py-1 text-sm text-red-700 hover:bg-red-50"
                      >
                        Delete
                      </button>
                    </div>
                  </li>
                )
              })}
            </ul>
          </section>
        </>
      )}
    </div>
  )
}