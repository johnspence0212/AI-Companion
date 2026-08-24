<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import {
  Bot,
  CalendarDays,
  FolderKanban,
  Inbox,
  KeyRound,
  Library,
  ShieldCheck,
  User,
  Users,
} from 'lucide-vue-next'
import { Separator } from '@/ui/separator'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/ui/sidebar'
import AppSidebar from './AppSidebar.vue'
import UserMenu from './UserMenu.vue'
import WorkbenchSearch from './WorkbenchSearch.vue'

const route = useRoute()

const routeInfo: Record<string, { label: string; icon: typeof CalendarDays }> = {
  home: { label: 'Today', icon: CalendarDays },
  inbox: { label: 'Inbox', icon: Inbox },
  projects: { label: 'Projects', icon: FolderKanban },
  library: { label: 'Library', icon: Library },
  'ai-clients': { label: 'AI Clients', icon: Bot },
  profile: { label: 'Profile', icon: User },
  users: { label: 'Users', icon: Users },
  roles: { label: 'Roles', icon: KeyRound },
  'security-audit': { label: 'Security audit', icon: ShieldCheck },
}

const currentRoute = computed(() => {
  const name = route.name as string
  return routeInfo[name] ?? { label: 'Page', icon: CalendarDays }
})

const breadcrumbs = computed(() => {
  if (route.meta.breadcrumbs) {
    return route.meta.breadcrumbs as Array<{ label: string; path: string }>
  }

  const name = route.name as string
  if (name === 'users' || name === 'roles' || name === 'security-audit') {
    return [
      { label: 'Settings', path: route.path },
      { label: routeInfo[name]!.label, path: route.path },
    ]
  }

  if (routeInfo[name]) {
    return [{ label: routeInfo[name].label, path: route.path }]
  }

  return []
})
</script>

<template>
  <SidebarProvider class="flex min-h-svh w-full flex-1">
    <AppSidebar />
    <SidebarInset>
      <header class="flex h-16 shrink-0 items-center gap-2 border-b px-4">
        <SidebarTrigger class="-ml-1 md:hidden" />
        <Separator orientation="vertical" class="mr-2 h-4 md:hidden" />
        <nav
          class="flex min-w-0 shrink-0 items-center gap-2 text-sm md:max-w-[40%]"
          aria-label="Breadcrumb"
        >
          <component :is="currentRoute.icon" class="h-4 w-4 shrink-0 text-muted-foreground" />
          <template v-for="(crumb, index) in breadcrumbs" :key="`${crumb.label}-${index}`">
            <span
              v-if="index < breadcrumbs.length - 1"
              class="truncate font-medium text-muted-foreground"
            >
              {{ crumb.label }}
            </span>
            <span v-else class="truncate font-medium">
              {{ crumb.label }}
            </span>
            <span v-if="index < breadcrumbs.length - 1" class="text-muted-foreground">/</span>
          </template>
        </nav>
        <WorkbenchSearch />
        <div class="flex shrink-0 items-center gap-2">
          <UserMenu />
        </div>
      </header>
      <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
        <slot />
      </div>
    </SidebarInset>
  </SidebarProvider>
</template>
