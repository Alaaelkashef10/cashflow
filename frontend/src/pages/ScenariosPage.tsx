import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import {
  useCreateSample,
  useCreateScenario,
  useDeleteScenario,
  useSamples,
  useScenarios,
} from '../api/hooks'
import { fieldErrors } from '../api/errors'
import { formatAmount } from '../format'

const inputClass =
  'mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-slate-500 focus:outline-none'

function FieldErrors({ messages }: { messages: string[] }) {
  if (messages.length === 0) return null
  return <p className="mt-1 text-sm text-red-700">{messages.join(' ')}</p>
}

export default function ScenariosPage() {
  const navigate = useNavigate()
  const scenarios = useScenarios()
  const samples = useSamples()
  const createScenario = useCreateScenario()
  const createSample = useCreateSample()
  const deleteScenario = useDeleteScenario()

  const [name, setName] = useState('')
  const [balance, setBalance] = useState('0')
  const [startDate, setStartDate] = useState(() => new Date().toISOString().slice(0, 10))

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    createScenario.mutate(
      { name, startingBalance: Number(balance), startDate },
      {
        onSuccess: (scenario) => {
          setName('')
          navigate(`/scenarios/${scenario.id}`)
        },
      },
    )
  }

  function handleDelete(id: number, scenarioName: string) {
    if (window.confirm(`Delete "${scenarioName}" and all of its entries?`)) {
      deleteScenario.mutate(id)
    }
  }

  const formError = createScenario.error

  return (
    <div className="space-y-8">
      <section>
        <h2 className="text-xl font-semibold">Start from a sample</h2>
        <p className="mt-1 text-sm text-slate-600">
          Each sample creates a ready-made scenario you can open and explore.
        </p>
        {samples.isPending && <p className="mt-3 text-sm text-slate-500">Loading samples...</p>}
        {samples.isError && (
          <p className="mt-3 text-sm text-red-700">Could not load samples: {samples.error.message}</p>
        )}
        <div className="mt-3 grid gap-3 sm:grid-cols-3">
          {samples.data?.map((sample) => (
            <button
              key={sample.key}
              type="button"
              disabled={createSample.isPending}
              onClick={() =>
                createSample.mutate(sample.key, {
                  onSuccess: (s) => navigate(`/scenarios/${s.id}`),
                })
              }
              className="rounded-lg border border-slate-200 bg-white p-3 text-left hover:border-slate-400 disabled:opacity-50"
            >
              <span className="block text-sm font-medium">{sample.name}</span>
              <span className="mt-1 block text-xs text-slate-600">{sample.description}</span>
            </button>
          ))}
        </div>
        {createSample.isError && (
          <p className="mt-2 text-sm text-red-700">{createSample.error.message}</p>
        )}
      </section>

      <section>
        <h2 className="text-xl font-semibold">New scenario</h2>
        <form onSubmit={handleSubmit} className="mt-3 space-y-4 rounded-lg border border-slate-200 bg-white p-4">
          <div>
            <label className="block text-sm font-medium" htmlFor="name">Name</label>
            <input id="name" className={inputClass} value={name} onChange={(e) => setName(e.target.value)} />
            <FieldErrors messages={fieldErrors(formError, 'name')} />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="block text-sm font-medium" htmlFor="balance">Starting balance</label>
              <input
                id="balance"
                type="number"
                step="0.01"
                className={inputClass}
                value={balance}
                onChange={(e) => setBalance(e.target.value)}
              />
              <FieldErrors messages={fieldErrors(formError, 'startingBalance')} />
            </div>
            <div>
              <label className="block text-sm font-medium" htmlFor="startDate">Start date</label>
              <input
                id="startDate"
                type="date"
                className={inputClass}
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
              <FieldErrors messages={fieldErrors(formError, 'startDate')} />
            </div>
          </div>
          {formError && Object.keys((formError as { errors?: object }).errors ?? {}).length === 0 && (
            <p className="text-sm text-red-700">{formError.message}</p>
          )}
          <button
            type="submit"
            disabled={createScenario.isPending}
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
          >
            {createScenario.isPending ? 'Creating...' : 'Create scenario'}
          </button>
        </form>
      </section>

      <section>
        <h2 className="text-xl font-semibold">Your scenarios</h2>
        {scenarios.isPending && <p className="mt-3 text-sm text-slate-500">Loading...</p>}
        {scenarios.isError && (
          <p className="mt-3 text-sm text-red-700">Could not load scenarios: {scenarios.error.message}</p>
        )}
        {scenarios.data?.length === 0 && (
          <p className="mt-3 text-sm text-slate-600">No scenarios yet. Create one above or start from a sample.</p>
        )}
        <ul className="mt-3 space-y-2">
          {scenarios.data?.map((s) => (
            <li
              key={s.id}
              className="flex items-center justify-between rounded-lg border border-slate-200 bg-white p-3"
            >
              <Link to={`/scenarios/${s.id}`} className="min-w-0 flex-1">
                <span className="block truncate font-medium">{s.name}</span>
                <span className="block text-sm text-slate-600">
                  Starts {s.startDate} · balance {formatAmount(s.startingBalance)} · {s.entryCount} entries
                </span>
              </Link>
              <button
                type="button"
                onClick={() => handleDelete(Number(s.id), s.name)}
                className="ml-3 rounded-md border border-slate-300 px-3 py-1 text-sm text-red-700 hover:bg-red-50"
              >
                Delete
              </button>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}