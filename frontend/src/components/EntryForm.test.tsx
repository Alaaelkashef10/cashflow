import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { components } from '../api/schema'
import EntryForm from './EntryForm'

// The form talks to the API only through these hooks, so the tests replace them with fakes.
const mocks = vi.hoisted(() => ({
  categorize: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
}))

vi.mock('../api/hooks', () => ({
  useCategorize: () => ({ mutate: mocks.categorize, isPending: false }),
  useCreateEntry: () => ({ mutate: mocks.create, isPending: false, error: null }),
  useUpdateEntry: () => ({ mutate: mocks.update, isPending: false, error: null }),
}))

type Entry = components['schemas']['EntryResponse']
type Suggestion = { category: string; confidence: number; source: string }

const existingEntry: Entry = {
  id: 7,
  scenarioId: 1,
  type: 'Bill',
  description: 'Rent',
  amount: 4000,
  date: '2026-11-01',
  category: 'Rent',
  aiSuggestedCategory: null,
  userCorrected: false,
}

function setup(entry?: Entry) {
  const user = userEvent.setup({ advanceTimers: (ms) => vi.advanceTimersByTime(ms) })
  render(<EntryForm scenarioId={1} defaultDate="2026-11-01" entry={entry} />)
  return user
}

// Lets the 400 ms typing delay pass.
async function pause(ms = 400) {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms)
  })
}

// Pretends the API answered the n-th suggestion request (the last one by default).
function answer(result: Suggestion, callIndex = mocks.categorize.mock.calls.length - 1) {
  const options = mocks.categorize.mock.calls[callIndex][1] as { onSuccess: (r: Suggestion) => void }
  act(() => options.onSuccess(result))
}

const netflix: Suggestion = { category: 'Subscriptions', confidence: 0.9, source: 'keyword' }

describe('EntryForm suggestions', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    mocks.categorize.mockClear()
    mocks.create.mockClear()
    mocks.update.mockClear()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('asks for a suggestion only after the user stops typing', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Netflix')
    expect(mocks.categorize).not.toHaveBeenCalled()

    await pause()
    expect(mocks.categorize).toHaveBeenCalledTimes(1)
    expect(mocks.categorize.mock.calls[0][0]).toBe('Netflix')
  })

  it('does not ask for descriptions shorter than three characters', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'ab')
    await pause()

    expect(mocks.categorize).not.toHaveBeenCalled()
  })

  it('pre-selects the suggested category and explains where it came from', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Netflix')
    await pause()
    answer(netflix)

    expect(screen.getByLabelText('Category')).toHaveValue('Subscriptions')
    expect(screen.getByText(/Suggested:/)).toHaveTextContent(
      'Suggested: Subscriptions (keyword rules, 90% confidence)',
    )
  })

  it('sends the suggestion along with the entry', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Netflix')
    await pause()
    answer(netflix)
    await user.type(screen.getByLabelText('Amount'), '150')
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    expect(mocks.create).toHaveBeenCalledWith(
      expect.objectContaining({
        type: 'Bill',
        description: 'Netflix',
        amount: 150,
        date: '2026-11-01',
        category: 'Subscriptions',
        aiSuggestedCategory: 'Subscriptions',
      }),
      expect.anything(),
    )
  })

  it("keeps the user's own choice when a suggestion arrives later", async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Spotify')
    await user.selectOptions(screen.getByLabelText('Category'), 'Groceries')
    await pause()
    answer(netflix)

    expect(screen.getByLabelText('Category')).toHaveValue('Groceries')
    expect(screen.getByRole('button', { name: 'Use suggestion' })).toBeInTheDocument()

    await user.type(screen.getByLabelText('Amount'), '10')
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    // The server compares these two values and records that the user corrected the AI.
    expect(mocks.create).toHaveBeenCalledWith(
      expect.objectContaining({ category: 'Groceries', aiSuggestedCategory: 'Subscriptions' }),
      expect.anything(),
    )
  })

  it('switches to the suggestion when the user clicks "Use suggestion"', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Spotify')
    await user.selectOptions(screen.getByLabelText('Category'), 'Groceries')
    await pause()
    answer(netflix)
    await user.click(screen.getByRole('button', { name: 'Use suggestion' }))

    expect(screen.getByLabelText('Category')).toHaveValue('Subscriptions')
  })

  it('does not send a suggestion that no longer matches the description', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Netflix')
    await pause()
    answer(netflix)
    await user.type(screen.getByLabelText('Description'), ' plan for the whole family')
    await user.type(screen.getByLabelText('Amount'), '150')
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    expect(mocks.create).toHaveBeenCalledWith(
      expect.objectContaining({ aiSuggestedCategory: null }),
      expect.anything(),
    )
  })

  it('ignores a late answer for text the user has already changed', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Net')
    await pause()
    await user.type(screen.getByLabelText('Description'), 'flix')

    answer(netflix, 0) // the answer to "Net" arrives after the user typed "Netflix"

    expect(screen.getByLabelText('Category')).toHaveValue('Uncategorized')
    expect(screen.queryByText(/Suggested:/)).not.toBeInTheDocument()
  })

  it('tells the user when there is no suggestion', async () => {
    const user = setup()

    await user.type(screen.getByLabelText('Description'), 'Vodafone recharge')
    await pause()
    answer({ category: 'Uncategorized', confidence: 0, source: 'keyword' })

    expect(screen.getByText(/No suggestion for this description/)).toBeInTheDocument()
    expect(screen.getByLabelText('Category')).toHaveValue('Uncategorized')
  })
})

describe('EntryForm when editing', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    mocks.categorize.mockClear()
    mocks.update.mockClear()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('shows the existing values and offers no suggestions', async () => {
    const user = setup(existingEntry)

    expect(screen.getByLabelText('Description')).toHaveValue('Rent')
    expect(screen.getByLabelText('Amount')).toHaveValue(4000)

    await user.type(screen.getByLabelText('Description'), ' November')
    await pause()

    expect(mocks.categorize).not.toHaveBeenCalled()
    expect(screen.queryByText(/Suggested:/)).not.toBeInTheDocument()
  })

  it('saves the changes against the right entry', async () => {
    const user = setup(existingEntry)

    await user.clear(screen.getByLabelText('Amount'))
    await user.type(screen.getByLabelText('Amount'), '4500')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(mocks.update).toHaveBeenCalledWith(
      { id: 7, body: expect.objectContaining({ amount: 4500, category: 'Rent', date: '2026-11-01' }) },
      expect.anything(),
    )
  })
})