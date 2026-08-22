import { existsSync, readFileSync } from 'node:fs'
import { resolve } from 'node:path'

const dist = resolve(import.meta.dirname, '../dist')
const manifestPath = resolve(dist, 'manifest.webmanifest')
const swPath = resolve(dist, 'sw.js')

const missing = [manifestPath, swPath].filter((path) => !existsSync(path))
if (missing.length > 0) {
  console.error('PWA production artifacts missing:', missing.join(', '))
  process.exit(1)
}

const sw = readFileSync(swPath, 'utf8')
if (!sw.includes('/api')) {
  console.error('Service worker must exclude /api from navigation fallback (cookie + CSRF).')
  process.exit(1)
}

console.log('PWA artifacts OK (manifest + sw.js, /api not used as SPA fallback).')
