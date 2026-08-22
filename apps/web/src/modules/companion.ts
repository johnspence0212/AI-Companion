import { CalendarDays, FolderKanban, Inbox, Library, Search } from 'lucide-vue-next'
import type { AppModule } from './registry'

export const companionModule: AppModule = {
  key: 'companion',
  routes: [
    {
      path: '/inbox',
      name: 'inbox',
      component: () => import('@/views/InboxView.vue'),
      meta: { requiresAuth: true, permission: 'inbox.read' },
    },
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
      path: '/search',
      name: 'search',
      component: () => import('@/views/SearchView.vue'),
      meta: { requiresAuth: true, permission: 'search.read' },
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
    { label: 'Inbox', to: '/inbox', icon: Inbox, permission: 'inbox.read' },
    { label: 'Projects', to: '/projects', icon: FolderKanban, permission: 'projects.read' },
    { label: 'Library', to: '/library', icon: Library, permission: 'documents.read' },
    { label: 'Search', to: '/search', icon: Search, permission: 'search.read' },
  ],
}
