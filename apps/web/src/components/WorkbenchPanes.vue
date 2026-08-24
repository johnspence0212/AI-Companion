<script setup lang="ts">
import { computed, useSlots } from 'vue'
import { cn } from '@/lib/utils'

defineProps<{
  systemView?: string
  navTitle?: string
  listTitle?: string
  detailTitle?: string
  /**
   * `workbench` is nav | list | reading pane.
   * `home` is a primary list with a narrower secondary column (Today).
   */
  layout?: 'workbench' | 'home'
}>()

const slots = useSlots()
const showNav = computed(() => typeof slots.nav === 'function')
const showDetail = computed(() => typeof slots.detail === 'function')
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col overflow-hidden" role="group" aria-label="Workbench">
    <p v-if="systemView" class="sr-only">View: {{ systemView }}</p>
    <div
      :class="
        cn(
          'flex min-h-0 flex-1 flex-col lg:flex-row',
          layout === 'home' ? 'overflow-y-auto lg:overflow-hidden' : 'overflow-hidden',
        )
      "
    >
      <section
        v-if="showNav"
        class="flex min-h-0 w-full shrink-0 flex-col overflow-hidden border-b lg:w-56 lg:border-r lg:border-b-0"
        aria-label="Navigation pane"
      >
        <header class="flex h-10 shrink-0 items-center justify-between gap-2 border-b px-3">
          <h2 class="truncate text-sm font-medium">{{ navTitle }}</h2>
          <div v-if="$slots['nav-actions']" class="flex shrink-0 items-center gap-2">
            <slot name="nav-actions" />
          </div>
        </header>
        <div class="min-h-0 flex-1 overflow-y-auto">
          <slot name="nav" />
        </div>
      </section>

      <section
        :class="
          cn(
            'flex min-h-0 min-w-0 flex-col overflow-hidden',
            showDetail && layout === 'home' && 'lg:min-h-0 lg:flex-1',
            showDetail &&
              layout !== 'home' &&
              'border-b lg:w-[22rem] lg:shrink-0 lg:border-r lg:border-b-0',
            !showDetail && 'flex-1',
          )
        "
        aria-label="List pane"
      >
        <header
          v-if="listTitle || $slots['list-actions']"
          class="flex h-10 shrink-0 items-center justify-between gap-2 border-b px-3"
        >
          <h2 class="truncate text-sm font-medium">{{ listTitle }}</h2>
          <div v-if="$slots['list-actions']" class="flex shrink-0 items-center gap-2">
            <slot name="list-actions" />
          </div>
        </header>
        <div v-if="$slots['list-toolbar']" class="shrink-0">
          <slot name="list-toolbar" />
        </div>
        <div class="min-h-0 flex-1 overflow-y-auto">
          <slot name="list" />
        </div>
      </section>

      <section
        v-if="showDetail"
        :class="
          cn(
            'flex min-h-0 min-w-0 flex-col overflow-hidden',
            layout === 'home' ? 'lg:w-80 lg:shrink-0 lg:border-l' : 'flex-1',
          )
        "
        aria-label="Detail pane"
      >
        <header class="flex h-10 shrink-0 items-center justify-between gap-2 border-b px-3">
          <h2 class="truncate text-sm font-medium">{{ detailTitle }}</h2>
          <div v-if="$slots['detail-actions']" class="flex shrink-0 items-center gap-2">
            <slot name="detail-actions" />
          </div>
        </header>
        <div class="min-h-0 flex-1 overflow-y-auto">
          <slot name="detail" />
        </div>
      </section>
    </div>
  </div>
</template>
