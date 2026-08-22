<script setup lang="ts">
import { computed, ref } from 'vue'
import { onClickOutside } from '@vueuse/core'
import { Check, ChevronDown } from 'lucide-vue-next'
import { cn } from '@/lib/utils'

export interface MultiSelectOption {
  value: number | string
  label: string
}

const props = withDefaults(
  defineProps<{
    modelValue: (number | string)[]
    options: MultiSelectOption[]
    placeholder?: string
    disabled?: boolean
    id?: string
    labelledBy?: string
  }>(),
  {
    placeholder: 'Select options',
    disabled: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: (number | string)[]]
}>()

const open = ref(false)
const root = ref<HTMLElement | null>(null)

onClickOutside(root, () => {
  open.value = false
})

const displayLabel = computed(() => {
  if (props.modelValue.length === 0) {
    return props.placeholder
  }

  const labels = props.options
    .filter((option) => props.modelValue.includes(option.value))
    .map((option) => option.label)

  if (labels.length === 0) {
    return props.placeholder
  }

  if (labels.length <= 2) {
    return labels.join(', ')
  }

  return `${labels.length} options selected`
})

function toggle(value: number | string) {
  const next = new Set(props.modelValue)
  if (next.has(value)) {
    next.delete(value)
  } else {
    next.add(value)
  }
  emit('update:modelValue', [...next])
}
</script>

<template>
  <div ref="root" class="relative w-full">
    <button
      :id="id"
      type="button"
      :aria-labelledby="labelledBy"
      :aria-expanded="open"
      :disabled="disabled"
      class="flex h-9 w-full items-center justify-between gap-2 text-sm outline-none disabled:cursor-not-allowed disabled:opacity-50"
      @click="open = !open"
    >
      <span
        class="truncate text-left"
        :class="modelValue.length === 0 ? 'text-muted-foreground' : 'text-foreground'"
      >
        {{ displayLabel }}
      </span>
      <ChevronDown
        class="size-4 shrink-0 opacity-50 transition-transform"
        :class="open ? 'rotate-180' : ''"
      />
    </button>

    <div
      v-if="open"
      class="absolute z-50 mt-1 max-h-56 w-full overflow-auto rounded-md border bg-background p-1 shadow-md"
    >
      <button
        v-for="option in options"
        :key="option.value"
        type="button"
        class="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm hover:bg-muted"
        @click="toggle(option.value)"
      >
        <span
          :class="
            cn(
              'flex size-4 shrink-0 items-center justify-center rounded-sm border border-input',
              modelValue.includes(option.value) &&
                'border-foreground bg-foreground text-background',
            )
          "
        >
          <Check v-if="modelValue.includes(option.value)" class="size-3" />
        </span>
        <span class="truncate">{{ option.label }}</span>
      </button>
      <p v-if="options.length === 0" class="px-2 py-1.5 text-sm text-muted-foreground">
        No options available.
      </p>
    </div>
  </div>
</template>
