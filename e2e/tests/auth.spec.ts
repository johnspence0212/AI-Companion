import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const adminEmail = 'admin@enterprisestarter.local'
const bootstrapPassword = 'AdminPassword123!'
const readyPassword = 'ReadyAdmin123!'

async function expectAccessible(page: import('@playwright/test').Page) {
  const results = await new AxeBuilder({ page }).analyze()
  expect(results.violations).toEqual([])
}

async function login(
  page: import('@playwright/test').Page,
  email: string,
  password: string,
) {
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  const responsePromise = page.waitForResponse(
    (response) => response.url().endsWith('/api/v1/auth/login') && response.request().method() === 'POST',
  )
  await page.getByRole('button', { name: 'Sign in' }).click()
  return (await responsePromise).ok()
}

test('redirects unauthenticated visitors to the neutral login shell', async ({ page }) => {
  await page.goto('/')
  await expect(page).toHaveURL(/\/login/)
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible()
  await expect(page.getByText('EnterpriseStarter')).toBeVisible()
  await expect(page.getByRole('link', { name: /create/i })).toHaveCount(0)
  await expectAccessible(page)
})

test('does not expose a public registration route', async ({ page }) => {
  await page.goto('/register')
  await expect(page).toHaveURL(/\/login/)
})

test('admin provisions a user and RBAC denies its administration access', async ({
  browser,
  page,
}) => {
  await page.goto('/login')
  if (!(await login(page, adminEmail, bootstrapPassword))) {
    expect(await login(page, adminEmail, readyPassword)).toBe(true)
  }

  await page.waitForURL((url) => ['/', '/change-password'].includes(url.pathname))
  if (page.url().includes('/change-password')) {
    await page.getByLabel('Current password').fill(bootstrapPassword)
    await page.getByLabel('New password', { exact: true }).fill(readyPassword)
    await page.getByLabel('Confirm new password').fill(readyPassword)
    await Promise.all([
      page.waitForURL((url) => url.pathname === '/'),
      page.getByRole('button', { name: 'Change password' }).click(),
    ])
  }

  await expect(page.getByRole('heading', { name: 'Today' })).toBeVisible()
  await expectAccessible(page)

  await page.getByRole('button', { name: 'Administration' }).click()
  await page.getByRole('menuitem', { name: 'Users' }).click()
  await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()
  await expectAccessible(page)

  const memberEmail = `member-${Date.now()}@example.local`
  const memberTemporaryPassword = 'TemporaryUser123!'
  const memberReadyPassword = 'PermanentUser123!'
  await page.getByRole('button', { name: 'Add user' }).click()
  await page.getByLabel('Email').fill(memberEmail)
  await page.getByLabel('Display name').fill('E2E Member')
  await page.getByLabel('Temporary password').fill(memberTemporaryPassword)
  await page.getByRole('button', { name: 'Roles' }).click()
  await page.getByRole('button', { name: 'Member' }).click()
  await page.getByRole('button', { name: 'Add user', exact: true }).click()
  await expect(page.getByText(memberEmail)).toBeVisible()

  const memberContext = await browser.newContext()
  const memberPage = await memberContext.newPage()
  await memberPage.goto('/login')
  expect(await login(memberPage, memberEmail, memberTemporaryPassword)).toBe(true)
  await memberPage.waitForURL((url) => url.pathname === '/change-password')
  await memberPage.getByLabel('Current password').fill(memberTemporaryPassword)
  await memberPage.getByLabel('New password', { exact: true }).fill(memberReadyPassword)
  await memberPage.getByLabel('Confirm new password').fill(memberReadyPassword)
  await Promise.all([
    memberPage.waitForURL((url) => url.pathname === '/'),
    memberPage.getByRole('button', { name: 'Change password' }).click(),
  ])
  await memberPage.goto('/admin/users')
  await expect(memberPage).toHaveURL(/\/$/)
  await expect(memberPage.getByRole('button', { name: 'Administration' })).toHaveCount(0)
  await memberContext.close()

  await page.getByRole('button', { name: 'Administration' }).click()
  await page.getByRole('menuitem', { name: 'Security audit' }).click()
  await expect(page.getByRole('heading', { name: 'Security audit' })).toBeVisible()
  await expect(page.getByText('admin.user.created').first()).toBeVisible()
})
