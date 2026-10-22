import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'

// Fails only on serious or critical WCAG A/AA problems; everything found is printed.
async function seriousViolations(page: Page) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()

  const describe = (v: (typeof results.violations)[number]) =>
    `${v.impact} | ${v.id} | ${v.help} | ${v.nodes.length} element(s), e.g. ${v.nodes[0]?.target.join(' ')}`

  results.violations.forEach((v) => console.log('AXE:', describe(v)))
  return results.violations
    .filter((v) => v.impact === 'serious' || v.impact === 'critical')
    .map(describe)
}

test('home page has no serious accessibility problems', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('button', { name: /Steady month/ })).toBeVisible()

  expect(await seriousViolations(page)).toEqual([])
})

test('scenario page with warning days has no serious accessibility problems', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('button', { name: /rent before payday/ }).click()
  await expect(page.getByRole('alert')).toContainText('below zero')

  expect(await seriousViolations(page)).toEqual([])
})