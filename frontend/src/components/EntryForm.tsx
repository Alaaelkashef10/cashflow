import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import type { components } from '../api/schema'
import { useCategorize, useCreateEntry, useUpdateEntry } from '../api/hooks'
import { fieldErrors, generalMessage } from '../api/errors'
import { CATEGORIES } from '../categories'

type Entry = components['schemas']['EntryResponse']

type Suggestion = {
  category: string
  confidence: number
  source: string
  forText: string
}

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

function sourceLabel(source: string) {
  return source === 'keyword' ? 'keyword rules' : source
}

export default function EntryForm({ scenarioId, defaultDate, entry, onDone, onCancel }: Props) {
  const create = useCreateEntry(scenarioId)
  const update = useUpdateEntry()
  const categorize = useCategorize()

  const [type, setType] = useState<Entry['type']>(entry?.type ?? 'Bill')
  const [description, setDescription] = useState(entry?.description ?? '')
  const [amount, setAmount] = useState(entry ? String(entry.amount) : '')
  const [date, setDate] = useState(entry?.date ?? defaultDate)
  const [category, setCategory] = useState<string>(entry?.category ?? 'Uncategorized')
  const [suggestion, setSuggestion] = useState<Suggestion | null>(null)

  // Refs hold values that the delayed callback needs to read at the moment it runs.
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)
  const latestText = useRef('')
  const categoryTouched = useRef(false)

  useEffect(() => () => clearTimeout(timer.current), [])

  const error = entry ? update.error : create.error
  const isPending = entry ? update.isPending : create.isPending
  const general = generalMessage(error)
  const idSuffix = entry?.id ?? 'new'

  function handleDescriptionChange(value: string) {
    setDescription(value)
    if (entry) return // suggestions are only for new entries

    const text = value.trim()
    latestText.current = text
    clearTimeout(timer.current)

    if (text.length < 3) {
      setSuggestion(null)
      return
    }

    timer.current = setTimeout(() => {
      categorize.mutate(text, {
        onSuccess: (result) => {
          if (latestText.current !== text) return // the user kept typing; ignore the old answer
          setSuggestion({
            category: result.category,
            confidence: Number(result.confidence),
            source: result.source,
            forText: text,
          })
          if (!categoryTouched.current) setCategory(result.category)
        },
      })
    }, 400)
  }

  function handleCategoryChange(value: string) {
    categoryTouched.current = true
    setCategory(value)
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const body = { type, description, amount: Number(amount), date, category }

    if (entry) {
      update.mutate({ id: Number(entry.id), body }, { onSuccess: () => onDone?.() })
      return
    }

    // Send the AI's suggestion only if it belongs to the description as it is now.
    const aiSuggestedCategory =
      suggestion && suggestion.forText === description.trim() ? suggestion.category : null

    create.mutate(
      { ...body, aiSuggestedCategory },
      {
        onSuccess: () => {
          clearTimeout(timer.current)
          latestText.current = ''
          categoryTouched.current = false
          setDescription('')
          setAmount('')
          setCategory('Uncategorized')
          setSuggestion(null)
          onDone?.()
        },
      },
    )
  }

  const hasUsefulSuggestion = suggestion !== null && suggestion.category !== 'Uncategorized'

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-slate-200 bg-white p-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="block text-sm font-medium" htmlFor={`type-${idSuffix}`}>Type</label>
          <select
            id={`type-${idSuffix}`}
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
          <label className="block text-sm font-medium" htmlFor={`desc-${idSuffix}`}>Description</label>
          <input
            id={`desc-${idSuffix}`}
            className={inputClass}
            value={description}
            onChange={(e) => handleDescriptionChange(e.target.value)}
          />
          <FieldErrors messages={fieldErrors(error, 'description')} />
        </div>

        <div>
          <label className="block text-sm font-medium" htmlFor={`amount-${idSuffix}`}>Amount</label>
          <input
            id={`amount-${idSuffix}`}
            type="number"
            step="0.01"
            className={inputClass}
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
          <FieldErrors messages={fieldErrors(error, 'amount')} />
        </div>

        <div>
          <label className="block text-sm font-medium" htmlFor={`date-${idSuffix}`}>Date</label>
          <input
            id={`date-${idSuffix}`}
            type="date"
            className={inputClass}
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
          <FieldErrors messages={fieldErrors(error, 'date')} />
        </div>

        <div className="sm:col-span-2">
          <label className="block text-sm font-medium" htmlFor={`cat-${idSuffix}`}>Category</label>
          <select
            id={`cat-${idSuffix}`}
            className={inputClass}
            value={category}
            onChange={(e) => handleCategoryChange(e.target.value)}
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
          <FieldErrors messages={fieldErrors(error, 'category')} />

          {!entry && (
            <p className="mt-2 text-sm text-slate-600" aria-live="polite">
              {categorize.isPending && 'Looking for a suggestion...'}
              {!categorize.isPending && hasUsefulSuggestion && (
                <>
                  Suggested: <strong>{suggestion.category}</strong> ({sourceLabel(suggestion.source)},{' '}
                  {Math.round(suggestion.confidence * 100)}% confidence).{' '}
                  {category !== suggestion.category && (
                    <button
                      type="button"
                      onClick={() => setCategory(suggestion.category)}
                      className="font-medium text-slate-900 underline"
                    >
                      Use suggestion
                    </button>
                  )}
                </>
              )}
              {!categorize.isPending && suggestion && !hasUsefulSuggestion && (
                <>No suggestion for this description. Please choose a category.</>
              )}
            </p>
          )}
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