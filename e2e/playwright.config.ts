import { defineConfig, devices } from '@playwright/test'

const webPort = process.env.WEB_PORT ?? '5173'
const apiPort = process.env.API_PORT ?? '5000'
const databaseConnection =
  process.env.Database__ConnectionString ??
  'Host=localhost;Port=5432;Database=enterprisestarter_e2e;Username=postgres;Password=postgres'

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'list',
  use: {
    baseURL: `http://localhost:${webPort}`,
    trace: 'on-first-retry'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ],
  webServer: [
    {
      command:
        'dotnet run --project ../apps/api/EnterpriseStarter.Api.csproj --no-launch-profile --urls http://localhost:' +
        apiPort,
      url: `http://localhost:${apiPort}/health/ready`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        Database__Provider: 'PostgreSQL',
        Database__ConnectionString: databaseConnection,
        WebOrigin: `http://localhost:${webPort}`,
        WebOrigins__0: `http://localhost:${webPort}`
      }
    },
    {
      command: 'npm run dev -- --port ' + webPort + ' --strictPort',
      cwd: '../apps/web',
      url: `http://localhost:${webPort}`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        VITE_API_BASE_URL: `http://localhost:${apiPort}/api/v1`,
        VITE_APP_NAME: 'EnterpriseStarter',
      }
    }
  ]
})
