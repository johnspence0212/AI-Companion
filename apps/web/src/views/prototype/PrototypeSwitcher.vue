<script setup lang="ts">
/**
 * PROTOTYPE only. Hidden from production builds.
 * Three variants of the V1 workbench, switchable via ?variant=.
 */
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Button } from '@/ui'

const labels = {
  A: 'A (Today-first)',
  B: 'B (Library-first)',
  C: 'C (Execution three-pane)',
} as const

type Variant = keyof typeof labels
const keys = Object.keys(labels) as Variant[]

const route = useRoute()
const router = useRouter()

const current = computed<Variant>(() => {
  const value = String(route.query.variant ?? 'A')
  return value === 'B' || value === 'C' ? value : 'A'
})

function setVariant(next: Variant) {
  void router.replace({ query: { ...route.query, variant: next } })
}

function cycle(delta: number) {
  const index = keys.indexOf(current.value)
  setVariant(keys[(index + delta + keys.length) % keys.length]!)
}
</script>

<template>
  <div
    v-if="import.meta.env.DEV"
    class="fixed bottom-4 left-1/2 z-50 flex -translate-x-1/2 items-center gap-2 rounded-full border bg-background px-3 py-2 shadow-lg"
  >
    <Button size="sm" variant="ghost" @click="cycle(-1)">←</Button>
    <span class="min-w-48 text-center text-sm font-medium">{{ labels[current] }}</span>
    <Button size="sm" variant="ghost" @click="cycle(1)">→</Button>
  </div>
</template>
