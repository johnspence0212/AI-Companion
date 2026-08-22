<script setup lang="ts">
import { computed, useId } from 'vue'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    label: string
    for?: string
    required?: boolean
    class?: string
  }>(),
  {
    required: false,
  },
)

const autoId = useId()
const fieldId = computed(() => props.for ?? autoId)
const legendId = computed(() => `${fieldId.value}-legend`)
</script>

<template>
  <fieldset
    :class="
      cn(
        'group m-0 min-w-0 rounded-md border border-input p-0 transition-colors focus-within:border-foreground',
        props.class,
      )
    "
  >
    <legend
      :id="legendId"
      class="ml-3 px-1 text-xs leading-none font-medium text-muted-foreground group-focus-within:text-foreground"
    >
      {{ label }}
      <span v-if="required" class="text-destructive" aria-hidden="true">*</span>
    </legend>

    <label :for="fieldId" class="sr-only">{{ label }}</label>

    <div
      class="-mt-1 flex h-9 items-center px-3.5 [&_[data-slot=input]]:!h-9 [&_[data-slot=input]]:!leading-normal [&_[data-slot=input]]:w-full [&_[data-slot=input]]:!rounded-none [&_[data-slot=input]]:!border-0 [&_[data-slot=input]]:bg-transparent [&_[data-slot=input]]:!px-0 [&_[data-slot=input]]:!py-0 [&_[data-slot=input]]:!shadow-none [&_[data-slot=input]]:!ring-0 [&_[data-slot=input]]:focus-visible:!border-transparent [&_[data-slot=input]]:focus-visible:!ring-0 [&_select]:!h-9 [&_select]:!leading-normal [&_select]:w-full [&_select]:!rounded-none [&_select]:!border-0 [&_select]:bg-transparent [&_select]:!px-0 [&_select]:!py-0 [&_select]:text-sm [&_select]:!shadow-none [&_select]:outline-none [&_select]:!ring-0 [&_select]:focus:!ring-0"
    >
      <slot :id="fieldId" :aria-labelledby="legendId" />
    </div>
  </fieldset>
</template>

<style scoped>
:deep(input[type='date']) {
  line-height: 1.25rem;
}

:deep(input[type='date']:not(:focus):invalid::-webkit-datetime-edit) {
  color: transparent;
}

:deep(input[type='date']:focus::-webkit-datetime-edit),
:deep(input[type='date']:valid::-webkit-datetime-edit) {
  color: inherit;
}
</style>
