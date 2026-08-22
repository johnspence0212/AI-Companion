<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { KeyRound, PanelLeft, Settings, ShieldCheck, Users } from 'lucide-vue-next'
import {
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuPortal,
  DropdownMenuRoot,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from 'reka-ui'
import { appName } from '@/config'
import { moduleNavigation } from '@/modules/registry'
import { useAuthStore } from '@/stores/auth'
import { cn } from '@/lib/utils'
import { Button } from '@/ui/button'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from '@/ui/sidebar'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const { isMobile, setOpenMobile, state, toggleSidebar } = useSidebar()

const coreItems = computed(() =>
  moduleNavigation.filter((item) => !item.permission || auth.hasPermission(item.permission)),
)

const adminItems = computed(() =>
  [
    { label: 'Users', to: '/admin/users', icon: Users, permission: 'users.read' },
    { label: 'Roles', to: '/admin/roles', icon: KeyRound, permission: 'roles.read' },
    {
      label: 'Security audit',
      to: '/admin/security-audit',
      icon: ShieldCheck,
      permission: 'audit.read',
    },
  ].filter((item) => auth.hasPermission(item.permission)),
)

const collapseLabel = computed(() => (state.value === 'collapsed' ? 'Expand' : 'Collapse'))
const adminActive = computed(() => adminItems.value.some((item) => route.path.startsWith(item.to)))

function closeSidebar() {
  if (isMobile.value) {
    setOpenMobile(false)
  }
}

function goAdmin(path: string) {
  closeSidebar()
  void router.push(path)
}
</script>

<template>
  <Sidebar collapsible="icon" role="navigation" aria-label="Primary navigation">
    <SidebarHeader
      class="h-16 flex-row items-center border-b border-sidebar-border px-4 py-0 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-2"
    >
      <router-link
        to="/"
        class="flex size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sm font-semibold text-sidebar-accent-foreground"
        :aria-label="appName"
        :title="appName"
        @click="closeSidebar"
      >
        E
      </router-link>
    </SidebarHeader>
    <SidebarContent>
      <SidebarGroup>
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem v-for="item in coreItems" :key="item.to">
              <SidebarMenuButton as-child :tooltip="item.label" :is-active="route.path === item.to">
                <router-link :to="item.to" @click="closeSidebar">
                  <component :is="item.icon" />
                  <span>{{ item.label }}</span>
                </router-link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>

      <SidebarGroup v-if="adminItems.length > 0" class="mt-auto">
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem>
              <DropdownMenuRoot>
                <DropdownMenuTrigger as-child>
                  <SidebarMenuButton
                    :is-active="adminActive"
                    aria-label="Administration"
                    tooltip="Settings"
                  >
                    <Settings />
                    <span>Settings</span>
                  </SidebarMenuButton>
                </DropdownMenuTrigger>
                <DropdownMenuPortal>
                  <DropdownMenuContent
                    side="right"
                    align="end"
                    :side-offset="8"
                    :class="
                      cn(
                        'z-50 min-w-56 overflow-hidden rounded-md border bg-popover p-1 text-popover-foreground shadow-md',
                        'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
                        'data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
                      )
                    "
                  >
                    <DropdownMenuLabel
                      class="px-2 py-1.5 text-xs font-medium text-muted-foreground"
                    >
                      Administration
                    </DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      v-for="item in adminItems"
                      :key="item.to"
                      class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none focus:bg-accent focus:text-accent-foreground"
                      @select="goAdmin(item.to)"
                    >
                      <component :is="item.icon" class="size-4" />
                      {{ item.label }}
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenuPortal>
              </DropdownMenuRoot>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>
    </SidebarContent>

    <SidebarFooter class="border-t border-sidebar-border">
      <Button
        type="button"
        variant="ghost"
        class="h-8 w-full justify-start gap-2 px-2 text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground group-data-[collapsible=icon]:size-8! group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:p-2!"
        :aria-label="`${collapseLabel} sidebar`"
        @click="toggleSidebar"
      >
        <PanelLeft class="size-4 shrink-0" />
        <span class="truncate group-data-[collapsible=icon]:hidden">{{ collapseLabel }}</span>
      </Button>
    </SidebarFooter>
  </Sidebar>
</template>
