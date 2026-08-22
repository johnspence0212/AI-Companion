import { createRouter, createWebHistory } from 'vue-router'
import type { RouteMeta } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { moduleRoutes } from '@/modules/registry'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    guest?: boolean
    permission?: string
    allowDuringPasswordChange?: boolean
    breadcrumbs?: Array<{ label: string; path: string }>
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { guest: true },
    },
    {
      path: '/change-password',
      name: 'change-password',
      component: () => import('@/views/ChangePasswordView.vue'),
      meta: { requiresAuth: true, allowDuringPasswordChange: true },
    },
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/prototype/workbench',
      name: 'prototype-workbench',
      component: () => import('@/views/prototype/WorkbenchPrototypeView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/profile',
      name: 'profile',
      component: () => import('@/views/ProfileView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/users',
      name: 'users',
      component: () => import('@/views/UsersView.vue'),
      meta: { requiresAuth: true, permission: 'users.read' },
    },
    {
      path: '/admin/roles',
      name: 'roles',
      component: () => import('@/views/RolesView.vue'),
      meta: { requiresAuth: true, permission: 'roles.read' },
    },
    {
      path: '/admin/security-audit',
      name: 'security-audit',
      component: () => import('@/views/SecurityAuditView.vue'),
      meta: { requiresAuth: true, permission: 'audit.read' },
    },
    ...moduleRoutes,
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.hydrated) {
    await auth.hydrate()
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.guest && auth.isAuthenticated) {
    return { name: auth.mustChangePassword ? 'change-password' : 'home' }
  }

  if (auth.isAuthenticated && auth.mustChangePassword && !to.meta.allowDuringPasswordChange) {
    return { name: 'change-password' }
  }

  const permission = (to.meta as RouteMeta).permission
  if (permission && !auth.hasPermission(permission)) {
    return { name: 'home' }
  }
})

export default router
