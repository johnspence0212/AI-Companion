const configuredName = import.meta.env.VITE_APP_NAME?.trim()

export const appName = configuredName || 'EnterpriseStarter'
