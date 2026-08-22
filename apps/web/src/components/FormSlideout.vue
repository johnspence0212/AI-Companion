<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Maximize2, Minimize2 } from 'lucide-vue-next'
import { Button } from '@/ui/button'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/ui/sheet'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    description?: string
    submitLabel?: string
    pending?: boolean
    allowFullscreen?: boolean
  }>(),
  {
    submitLabel: 'Save',
    pending: false,
    allowFullscreen: false,
  },
)

const emit = defineEmits<{
  'update:open': [value: boolean]
  submit: []
}>()

const isFullscreen = ref(false)

const headerPaddingClass = computed(() => (props.allowFullscreen ? 'pr-20' : 'pr-10'))

watch(
  () => props.open,
  (open) => {
    if (!open) {
      isFullscreen.value = false
    }
  },
)

function setOpen(value: boolean) {
  emit('update:open', value)
}

function onSubmit() {
  if (!props.pending) {
    emit('submit')
  }
}
</script>

<template>
  <Sheet :open="open" @update:open="setOpen">
    <SheetContent
      side="right"
      :class="
        isFullscreen
          ? 'flex w-screen max-w-none flex-col gap-0 p-0 sm:max-w-none'
          : 'flex w-full max-w-md flex-col gap-0 p-0 sm:max-w-md'
      "
    >
      <template #header-actions>
        <Button
          v-if="allowFullscreen"
          type="button"
          variant="ghost"
          size="icon"
          class="size-8"
          :aria-label="isFullscreen ? 'Exit full screen' : 'Full screen'"
          @click="isFullscreen = !isFullscreen"
        >
          <Minimize2 v-if="isFullscreen" class="size-4" />
          <Maximize2 v-else class="size-4" />
        </Button>
      </template>

      <form class="flex h-full min-h-0 flex-1 flex-col" @submit.prevent="onSubmit">
        <SheetHeader class="border-b px-6 py-4 text-left">
          <div :class="['min-w-0 space-y-1', headerPaddingClass]">
            <SheetTitle>{{ title }}</SheetTitle>
            <SheetDescription v-if="description">{{ description }}</SheetDescription>
          </div>
        </SheetHeader>

        <div
          class="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto px-6 py-5 [&>.form-section+.form-section]:border-t [&>.form-section+.form-section]:border-border [&>.form-section+.form-section]:pt-4"
        >
          <slot />
        </div>

        <SheetFooter class="border-t px-6 py-4 sm:flex-row sm:justify-end">
          <Button type="button" variant="outline" :disabled="pending" @click="setOpen(false)">
            Cancel
          </Button>
          <Button type="submit" :disabled="pending">
            {{ submitLabel }}
          </Button>
        </SheetFooter>
      </form>
    </SheetContent>
  </Sheet>
</template>
