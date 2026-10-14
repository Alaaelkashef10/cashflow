import { useState } from 'react'
import type { FormEvent } from 'react'
import type { components } from '../api/schema'
import { useCreateEntry, useUpdateEntry } from '../api/hooks'
import { fieldErrors, generalMessage } from '../api/errors'
import { CATEGORIES } from '../categories'

type Entry = components['schemas']['EntryResponse']

type Props = {
  scenarioId: number
  defaultDate: string
  entry?: Entry
  onDone?: () => void
  onCancel?: () => void
}

const inputClass =
  'mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-slate-500 focus:outline-none'

function FieldErrors({ messages }: { messages: string[] }) {
  if (messages.length === 0) return null
  return <p className="mt-1 text-sm text-red-700">{messages.join(' ')}</p>
}

export default function EntryForm({ scenarioId, defaultDate, entry, onDone, onCancel }: Props) {
  const create = useCreateEntry(scenarioId)
  const update = useUpdateEntry()

  const [type, setType] = useState<Entry['type']>(entry?.type ?? 'Bill')
  const [description, setDescription] = useState(entry?.description ?? '')
  const [amount, setAmount] = useState(entry ? String(entry.amount) : '')
  const [date, setDate] = useState(entry?.date ?? defaultDate)
  const [category, setCategory] = useState<string>(entry?.category ?? 'Uncategorized')

  const error = entry ? update.error : create.error
  const isPending = entry ? update.isPending : create.isPending
  const general = generalMessage(error)

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const body = { type, description, amount: Number(amount), date, category }

    if (entry) {
      update.mutate({ id: Number(entry.id), body }, { onSuccess: () => onDone?.() })
    } else {
      create.mutate(
        { ...body, aiSuggestedCategory: null },
        {
          onSuccess: () => {
            setDescription('')
            setAmount('')
            onDone?.()
          },
        },
      )
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-slate-200 bg-white p-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="block text-sm font-medium" htmlFor={`type-${entry?.id ?? 'new'}`}>Type</label>
          <select
            id={`type-${entry?.id ?? 'new'}`}
            className={inputClass}
            value={type}
            onChange={(e) => setType(e.target.value as Entry['type'])}
          >
            <option value="Bill">Bill (money out)</option>
            <option value="Income">Income (money in)</option>
          </select>
          <FieldErrors messages={fieldErrors(error, 'type')} />
        </div>
        <div>
          <label className="block text-sm font-medium" htmlFor={`desc-${entry?.id ?? 'new'}`}>Description</label>
          <input
            id={`desc-${entry?.id ?? 'new'}`}
            className={inputClass}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <FieldErrors messages={fieldErrors(error, 'description')} />
        </div>
        <div>
          <label className="block text-sm font-medium" htmlFor={`amount-${entry?.id ?? 'new'}`}>Amount</label>
          <input
            id={`amount-${entry?.id ?? 'new'}`}
            type="number"
            step="0.01"
            className={inputClass}
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
          <FieldErrors messages={fieldErrors(error, 'amount')} />
        </div>
        <div>
          <label className="block text-sm font-medium" htmlFor={`date-${entry?.id ?? 'new'}`}>Date</label>
          <input
            id={`date-${entry?.id ?? 'new'}`}
            type="date"
            className={inputClass}
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
          <FieldErrors messages={fieldErrors(error, 'date')} />
        </div>
        <div className="sm:col-span-2">
          <label className="block text-sm font-medium" htmlFor={`cat-${entry?.id ?? 'new'}`}>Category</label>
          <select
            id={`cat-${entry?.id ?? 'new'}`}
            className={inputClass}
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
          <FieldErrors messages={fieldErrors(error, 'category')} />
        </div>
      </div>

      {general && <p className="text-sm text-red-700">{general}</p>}

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={isPending}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
        >
          {isPending ? 'Saving...' : entry ? 'Save changes' : 'Add entry'}
        </button>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="rounded-md border border-slate-300 px-4 py-2 text-sm hover:bg-slate-100"
          >
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}