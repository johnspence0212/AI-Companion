<script setup lang="ts">
/**
 * Centered guest/auth layout (login, forced password change).
 */
import { Download } from 'lucide-vue-next'
import { Button } from '@/ui/button'
import { usePwaInstall } from '@/lib/pwaInstall'

const { canInstall, install } = usePwaInstall()
</script>

<template>
  <main class="relative flex min-h-svh w-full flex-1 items-center justify-center bg-muted/40 p-4">
    <Button
      v-if="canInstall"
      type="button"
      variant="outline"
      class="absolute top-4 right-4"
      shape="square"
      @click="install"
    >
      <Download class="size-4" />
      Install app
    </Button>
    <slot />
  </main>
</template>
