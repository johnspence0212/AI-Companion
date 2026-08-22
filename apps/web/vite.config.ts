import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import { VitePWA } from 'vite-plugin-pwa'
import { pwaBackgroundColor, pwaThemeColor } from './src/lib/pwaPolicy'

const apiProxyTarget =
  process.env.VITE_API_PROXY_TARGET?.replace(/\/api(?:\/v1)?\/?$/, '') ?? 'http://localhost:5000'

const appName = process.env.VITE_APP_NAME?.trim() || 'EnterpriseStarter'
const pwaInDev = process.env.VITE_PWA_DEV === 'true' || process.argv.includes('pwa-dev')

// https://vite.dev/config/
export default defineConfig({
  root: fileURLToPath(new URL('./', import.meta.url)),
  plugins: [
    vue(),
    ...(process.env.VITE_ENABLE_DEVTOOLS === 'true' ? [vueDevTools()] : []),
    tailwindcss(),
    VitePWA({
      // Default `npm run dev` / Aspire: no service worker.
      // Opt in with `npm run dev:pwa` to test install locally.
      registerType: 'autoUpdate',
      injectRegister: 'script',
      includeAssets: ['favicon.ico', 'apple-touch-icon.png', 'pwa-192.png', 'pwa-512.png'],
      manifest: {
        name: appName,
        short_name: appName,
        description: 'Enterprise application',
        theme_color: pwaThemeColor,
        background_color: pwaBackgroundColor,
        display: 'standalone',
        start_url: '/',
        scope: '/',
        icons: [
          {
            src: 'pwa-192.png',
            sizes: '192x192',
            type: 'image/png',
          },
          {
            src: 'pwa-512.png',
            sizes: '512x512',
            type: 'image/png',
          },
          {
            src: 'pwa-512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      },
      workbox: {
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api(?:\/|$)/],
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2,webmanifest}'],
      },
      devOptions: {
        enabled: pwaInDev,
        type: 'module',
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: apiProxyTarget,
        changeOrigin: true,
        // Keep Set-Cookie host-agnostic for same-origin cookie auth via the Vite proxy.
        configure: (proxy) => {
          proxy.on('proxyRes', (proxyRes) => {
            const setCookie = proxyRes.headers['set-cookie']
            if (!setCookie) return
            proxyRes.headers['set-cookie'] = setCookie.map((cookie) =>
              cookie.replace(/;\s*Domain=[^;]+/i, ''),
            )
          })
        },
      },
    },
  },
})
