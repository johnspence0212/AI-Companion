import { Library } from 'lucide-vue-next'
import type { AppModule } from './registry'

export const companionModule: AppModule = {
  key: 'companion',
  routes: [
    {
      path: '/library',
      name: 'library',
      component: () => import('@/views/LibraryView.vue'),
      meta: { requiresAuth: true, permission: 'documents.read' },
    },
  ],
  navigation: [{ label: 'Library', to: '/library', icon: Library, permission: 'documents.read' }],
}
