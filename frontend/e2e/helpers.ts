import { expect } from '@playwright/test'
import type { Page } from '@playwright/test'

export function entriesSection(page: Page) {
  return page.locator('section').filter({
    has: page.getByRole('heading', { name: 'Entries', exact: true }),
  })
}

export function addEntryForm(page: Page) {
  return page.locator('form').filter({ has: page.getByRole('button', { name: 'Add entry' }) })
}

export async function createScenario(page: Page, name: string, balance: string, startDate: string) {
  await page.goto('/')
  await page.getByLabel('Name', { exact: true }).fill(name)
  await page.getByLabel('Starting balance').fill(balance)
  await page.getByLabel('Start date').fill(startDate)
  await page.getByRole('button', { name: 'Create scenario' }).click()
  await expect(page.getByRole('heading', { name, exact: true })).toBeVisible()
}

type NewEntry = {
  type: 'Bill' | 'Income'
  description: string
  amount: string
  date: string
  category: string
}

export async function addEntry(page: Page, entry: NewEntry) {
  const form = addEntryForm(page)
  await form.getByLabel('Type', { exact: true }).selectOption(entry.type)
  await form.getByLabel('Description', { exact: true }).fill(entry.description)
  // Choosing the category ourselves means a late AI suggestion can never overwrite it.
  await form.getByLabel('Category', { exact: true }).selectOption(entry.category)
  await form.getByLabel('Amount', { exact: true }).fill(entry.amount)
  await form.getByLabel('Date', { exact: true }).fill(entry.date)
  await form.getByRole('button', { name: 'Add entry' }).click()

  await expect(entriesSection(page).getByText(entry.description, { exact: true })).toBeVisible()
}