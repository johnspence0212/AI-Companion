import { CalendarDays, FolderKanban, Library } from 'lucide-vue-next'
import type { AppModule } from './registry'

export const companionModule: AppModule = {
  key: 'companion',
  routes: [
    {
      path: '/projects/:idOrSlug?',
      name: 'projects',
      component: () => import('@/views/ProjectsView.vue'),
      meta: { requiresAuth: true, permission: 'projects.read' },
    },
    {
      path: '/library',
      name: 'library',
      component: () => import('@/views/LibraryView.vue'),
      meta: { requiresAuth: true, permission: 'documents.read' },
    },
    {
      path: '/inbox',
      redirect: '/',
    },
    {
      path: '/search',
      redirect: '/',
    },
    {
      path: '/ai-clients',
      name: 'ai-clients',
      component: () => import('@/views/AiClientsView.vue'),
      meta: { requiresAuth: true, permission: 'aiclients.manage' },
    },
  ],
  navigation: [
    { label: 'Today', to: '/', icon: CalendarDays, permission: 'daily.read' },
    { label: 'Projects', to: '/projects', icon: FolderKanban, permission: 'projects.read' },
    { label: 'Library', to: '/library', icon: Library, permission: 'documents.read' },
  ],
}
