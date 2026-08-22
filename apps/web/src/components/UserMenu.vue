<script setup lang="ts">
import { computed } from 'vue'
import { Download, LogOut, User } from 'lucide-vue-next'
import { useRouter } from 'vue-router'
import {
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuPortal,
  DropdownMenuRoot,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from 'reka-ui'
import { Button } from '@/ui/button'
import { cn } from '@/lib/utils'
import { usePwaInstall } from '@/lib/pwaInstall'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const { canInstall, install } = usePwaInstall()

const initials = computed(() => {
  const displayName = auth.user?.displayName?.trim()
  if (displayName) {
    const parts = displayName.split(/\s+/).filter(Boolean)
    if (parts.length >= 2) {
      return `${parts[0]![0]}${parts[1]![0]}`.toUpperCase()
    }
    return displayName.slice(0, 2).toUpperCase()
  }

  const email = auth.user?.email ?? ''
  const local = email.split('@')[0] ?? ''
  if (local.length >= 2) return local.slice(0, 2).toUpperCase()
  return local.slice(0, 1).toUpperCase() || '?'
})

async function signOut() {
  await auth.logout()
  await router.push({ name: 'login' })
}

function goProfile() {
  void router.push('/profile')
}
</script>

<template>
  <DropdownMenuRoot v-if="auth.user">
    <DropdownMenuTrigger as-child>
      <Button
        variant="outline"
        size="icon"
        class="size-9 rounded-full"
        :aria-label="`Account menu for ${auth.user.email}`"
      >
        <span class="text-xs font-medium">{{ initials }}</span>
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuPortal>
      <DropdownMenuContent
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
        <DropdownMenuLabel class="px-2 py-1.5 font-normal">
          <p class="truncate text-sm font-medium">{{ auth.user.email }}</p>
          <p v-if="auth.user.displayName" class="truncate text-xs text-muted-foreground">
            {{ auth.user.displayName }}
          </p>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none focus:bg-accent focus:text-accent-foreground"
          @select="goProfile"
        >
          <User class="size-4" />
          Profile
        </DropdownMenuItem>
        <DropdownMenuItem
          v-if="canInstall"
          class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none focus:bg-accent focus:text-accent-foreground"
          @select="install"
        >
          <Download class="size-4" />
          Install app
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          class="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none focus:bg-accent focus:text-accent-foreground"
          @select="signOut"
        >
          <LogOut class="size-4" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenuPortal>
  </DropdownMenuRoot>
</template>
