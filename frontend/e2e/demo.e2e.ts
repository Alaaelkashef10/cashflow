import { expect, test } from '@playwright/test'
import { addEntry, addEntryForm, createScenario, entriesSection } from './helpers'

test('SRS demo: moving the rent removes the warning days', async ({ page }) => {
  // Fixed dates, so the test gives the same result on any day.
  await createScenario(page, 'Sara - January', '1000', '2027-01-01')
  await addEntry(page, {
    type: 'Bill', description: 'Rent', amount: '4000', date: '2027-01-01', category: 'Rent',
  })
  await addEntry(page, {
    type: 'Income', description: 'Client payment', amount: '6000', date: '2027-01-05',
    category: 'Salary/Client Payment',
  })

  // The balance dips below zero from Jan 1 to Jan 4.
  await expect(page.getByRole('alert')).toContainText('below zero on 4 days')
  await expect(
    page.getByRole('button', { name: /January 1, 2027.*Warning: below zero/ }),
  ).toBeVisible()
  await expect(page.getByRole('button', { name: /January 5, 2027/ })).not.toHaveAccessibleName(
    /Warning/,
  )

  // Move the rent to Jan 6.
  const rentRow = entriesSection(page).getByRole('listitem').filter({ hasText: 'Rent' })
  await rentRow.getByRole('button', { name: 'Edit' }).click()
  const editForm = page
    .locator('form')
    .filter({ has: page.getByRole('button', { name: 'Save changes' }) })
  await editForm.getByLabel('Date', { exact: true }).fill('2027-01-06')
  await editForm.getByRole('button', { name: 'Save changes' }).click()

  // The warnings disappear without a page reload.
  await expect(page.getByText('No days below zero in this projection.')).toBeVisible()
  await expect(page.getByRole('alert')).toHaveCount(0)

  // And the change survived: reload and check again.
  await page.reload()
  await expect(page.getByText('No days below zero in this projection.')).toBeVisible()
})

test('AI suggestion can be corrected, and the correction is remembered', async ({ page }) => {
  await createScenario(page, 'AI test', '500', '2027-01-01')
  const form = addEntryForm(page)

  await form.getByLabel('Description', { exact: true }).fill('Netflix')
  await expect(form.getByText(/Suggested:\s*Subscriptions/)).toBeVisible()
  await expect(form.getByLabel('Category', { exact: true })).toHaveValue('Subscriptions')

  // The user disagrees with the suggestion.
  await form.getByLabel('Category', { exact: true }).selectOption('Groceries')
  await form.getByLabel('Amount', { exact: true }).fill('150')
  await form.getByRole('button', { name: 'Add entry' }).click()

  await expect(entriesSection(page)).toContainText('AI suggested Subscriptions, you changed it')
  await page.reload()
  await expect(entriesSection(page)).toContainText('AI suggested Subscriptions, you changed it')
})

test('a sample scenario opens with its projection', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('button', { name: /Steady month/ }).click()

  await expect(page.getByRole('heading', { name: 'Steady month' })).toBeVisible()
  await expect(page.getByText('No days below zero in this projection.')).toBeVisible()
  await expect(page.getByText('700.00', { exact: true })).toBeVisible() // lowest balance
})