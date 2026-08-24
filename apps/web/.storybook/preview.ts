import type { Preview } from '@storybook/vue3-vite'
import { setup } from '@storybook/vue3-vite'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { User } from '@/api/types/schema'
import { useAuthStore } from '@/stores/auth'
import '../src/styles/index.css'
import './preview.css'

const EmptyRoute = { name: 'CatalogueRoute', template: '<div />' }

const catalogueRouter = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/', name: 'home', component: EmptyRoute },
    { path: '/login', name: 'login', component: EmptyRoute },
    { path: '/profile', name: 'profile', component: EmptyRoute },
    { path: '/change-password', name: 'change-password', component: EmptyRoute },
    { path: '/admin/users', name: 'users', component: EmptyRoute },
    { path: '/admin/roles', name: 'roles', component: EmptyRoute },
    { path: '/admin/security-audit', name: 'security-audit', component: EmptyRoute },
  ],
})

const catalogueAdmin: User = {
  id: 'catalogue-admin',
  email: 'admin@enterprisestarter.local',
  displayName: 'Ada Admin',
  isActive: true,
  mustChangePassword: false,
  createdAt: '2026-01-01T00:00:00Z',
  lastLoginAt: '2026-08-15T00:00:00Z',
  roles: ['Admin'],
  permissions: [
    'users.read',
    'users.manage',
    'roles.read',
    'roles.manage',
    'audit.read',
    'search.read',
  ],
}

setup((app) => {
  app.use(createPinia())
  app.use(catalogueRouter)
})

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    a11y: {
      test: 'todo',
    },
  },
  decorators: [
    (story, context) => ({
      components: { story },
      async setup() {
        const auth = useAuthStore()
        auth.user = catalogueAdmin
        auth.hydrated = true
        auth.error = null
        auth.loading = false

        const path = (context.parameters.catalogueRoute as string | undefined) ?? '/'
        if (catalogueRouter.currentRoute.value.path !== path) {
          await catalogueRouter.replace(path)
        }
      },
      template: '<story />',
    }),
  ],
}

export default preview
