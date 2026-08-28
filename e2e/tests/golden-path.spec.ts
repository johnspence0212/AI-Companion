import { expect, test, type Page } from '@playwright/test'

const adminEmail = 'admin@enterprisestarter.local'
const bootstrapPassword = 'AdminPassword123!'
const readyPassword = 'ReadyAdmin123!'

async function login(page: Page, email: string, password: string) {
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  const responsePromise = page.waitForResponse(
    (response) => response.url().endsWith('/api/v1/auth/login') && response.request().method() === 'POST',
  )
  await page.getByRole('button', { name: 'Sign in' }).click()
  return (await responsePromise).ok()
}

async function loginAdmin(page: Page) {
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
}

async function provisionOwner(page: Page, displayName: string) {
  const email = `${displayName.toLowerCase().replace(/\s+/g, '-')}-${Date.now()}@example.local`
  const temporaryPassword = 'TemporaryUser123!'
  const readyMemberPassword = 'PermanentUser123!'
  await page.getByRole('button', { name: 'Administration' }).click()
  await page.getByRole('menuitem', { name: 'Users' }).click()
  await expect(page.getByRole('heading', { name: 'Users', level: 1 })).toBeVisible()
  await page.getByRole('button', { name: 'Add user' }).click()
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Display name').fill(displayName)
  await page.getByLabel('Temporary password').fill(temporaryPassword)
  await page.getByRole('button', { name: 'Roles' }).click()
  await page.getByRole('button', { name: 'Member' }).click()
  const created = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/v1/users') &&
      response.request().method() === 'POST' &&
      response.ok(),
  )
  await page.getByRole('button', { name: 'Add user', exact: true }).click()
  await created
  await page.keyboard.press('Escape')
  return { email, temporaryPassword, readyMemberPassword }
}

function dialog(page: Page, title: string) {
  return page.getByRole('dialog', { name: title })
}

function dialogMarkdown(page: Page, title: string) {
  return dialog(page, title).locator('textarea[name="markdown"]')
}

async function loginNewMember(
  page: Page,
  email: string,
  temporaryPassword: string,
  readyMemberPassword: string,
) {
  await page.goto('/login')
  expect(await login(page, email, temporaryPassword)).toBe(true)
  await page.waitForURL((url) => url.pathname === '/change-password')
  await page.getByLabel('Current password').fill(temporaryPassword)
  await page.getByLabel('New password', { exact: true }).fill(readyMemberPassword)
  await page.getByLabel('Confirm new password').fill(readyMemberPassword)
  await Promise.all([
    page.waitForURL((url) => url.pathname === '/'),
    page.getByRole('button', { name: 'Change password' }).click(),
  ])
}

test('owner walks the UI golden path; a second owner sees nothing', async ({ browser, page }) => {
  test.setTimeout(60_000)
  const token = `kalshiuipath${Date.now()}`
  const fence = `# Notes\n\n\`\`\`python\ndef greet(name):\n    return f"hello {name}"\n${token}\n\`\`\``
  const documentTitle = `UI fence ${token}`
  const projectName = `Golden ${token}`
  const inboxTitle = `Inbox ${token}`

  await loginAdmin(page)
  await expect(page.getByRole('link', { name: 'Today' })).toBeVisible()
  const owner = await provisionOwner(page, 'Golden Owner')
  const other = await provisionOwner(page, 'Second Owner')

  const ownerPage = await (await browser.newContext()).newPage()
  await loginNewMember(ownerPage, owner.email, owner.temporaryPassword, owner.readyMemberPassword)
  await expect(ownerPage.getByRole('link', { name: 'Today' })).toBeVisible()

  await ownerPage.getByRole('link', { name: 'Projects' }).click()
  await ownerPage.getByRole('button', { name: 'New' }).first().click()
  await dialog(ownerPage, 'New Project').locator('input[name="project-name"]').fill(projectName)
  await dialog(ownerPage, 'New Project').getByRole('button', { name: 'Create' }).click()
  await expect(ownerPage.getByText('Project created.')).toBeVisible()
  await ownerPage.getByRole('button', { name: 'Edit', exact: true }).click()
  await dialog(ownerPage, 'Project Context').locator('input[name="context-title"]').fill('Golden Context')
  await dialogMarkdown(ownerPage, 'Project Context').fill(fence)
  await dialog(ownerPage, 'Project Context').getByRole('button', { name: 'Save' }).click()
  await expect(ownerPage.getByText('Project Context saved.')).toBeVisible()

  await ownerPage.getByRole('link', { name: 'Library' }).click()
  await ownerPage.getByRole('button', { name: 'New note' }).click()
  await ownerPage.locator('input[name="title"]').fill(documentTitle)
  await ownerPage.locator('textarea[name="markdown"]').fill(fence)
  await ownerPage.getByRole('button', { name: 'Save' }).click()
  await expect(ownerPage.getByText('Saved.')).toBeVisible()
  await expect(ownerPage.locator('textarea[name="markdown"]')).toHaveValue(fence)

  await ownerPage.locator('textarea[name="markdown"]').fill(`${fence}\nMore.\n`)
  await ownerPage.getByRole('button', { name: 'Save' }).click()
  await expect(ownerPage.getByRole('button', { name: 'Restore' })).toHaveCount(2)
  await ownerPage.getByRole('button', { name: 'Restore' }).first().click()
  await expect(ownerPage.getByText('Restored a new current revision.')).toBeVisible()
  await expect(ownerPage.locator('textarea[name="markdown"]')).toHaveValue(fence)

  await ownerPage.getByRole('textbox', { name: 'Search' }).fill(token)
  await ownerPage.getByRole('textbox', { name: 'Search' }).press('Enter')
  await expect(ownerPage.getByRole('dialog', { name: 'Search' })).toBeVisible()
  await dialog(ownerPage, 'Search').getByText(documentTitle, { exact: true }).click()
  await dialog(ownerPage, documentTitle).getByRole('button', { name: 'Source' }).click()
  await expect(dialogMarkdown(ownerPage, documentTitle)).toHaveValue(fence)
  await dialog(ownerPage, documentTitle).getByRole('button', { name: 'Close', exact: true }).first().click()

  await ownerPage.getByRole('link', { name: 'Inbox' }).click()
  await ownerPage.locator('input[name="inbox-capture"]').fill(`Process this thought ${token}`)
  await ownerPage.getByRole('button', { name: 'Capture' }).click()
  await expect(ownerPage.getByText('Captured.')).toBeVisible()
  await dialog(ownerPage, 'Process Inbox Item').locator('input[name="inbox-title"]').fill(inboxTitle)
  await dialog(ownerPage, 'Process Inbox Item').getByRole('button', { name: 'Process' }).click()
  await expect(ownerPage.getByText('Processed to a Document.')).toBeVisible()

  await ownerPage.getByRole('link', { name: 'Library' }).click()
  await expect(ownerPage.getByText(inboxTitle, { exact: true })).toBeVisible()

  await ownerPage.getByRole('link', { name: 'Today' }).click()
  await expect(ownerPage.getByRole('link', { name: 'Today' })).toBeVisible()
  const dailyText = `Daily custom ${token}`
  await ownerPage.locator('input[name="daily-item"]').fill(dailyText)
  await ownerPage.getByRole('button', { name: 'Add' }).click()
  await expect(ownerPage.getByText(dailyText, { exact: true })).toBeVisible()
  await ownerPage.getByRole('button', { name: 'Complete' }).click()
  await expect(ownerPage.getByText('Custom Daily Item · done')).toBeVisible()

  const otherPage = await (await browser.newContext()).newPage()
  await loginNewMember(otherPage, other.email, other.temporaryPassword, other.readyMemberPassword)
  await otherPage.getByRole('link', { name: 'Library' }).click()
  await expect(otherPage.getByText(documentTitle, { exact: true })).toHaveCount(0)
  await otherPage.getByRole('textbox', { name: 'Search' }).fill(token)
  await otherPage.getByRole('textbox', { name: 'Search' }).press('Enter')
  await expect(otherPage.getByText('No matches.')).toBeVisible()
  await expect(otherPage.getByText(documentTitle, { exact: true })).toHaveCount(0)
})
