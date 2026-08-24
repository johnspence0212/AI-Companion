<script setup lang="ts">
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'

withDefaults(
  defineProps<{
    modelValue: string
    placeholder?: string
    submitLabel?: string
    pending?: boolean
    name?: string
  }>(),
  {
    placeholder: '',
    submitLabel: 'Add',
    pending: false,
    name: 'composer',
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
  submit: []
}>()

function onSubmit() {
  emit('submit')
}
</script>

<template>
  <form class="flex items-center gap-2 border-b px-3 py-2" @submit.prevent="onSubmit">
    <Input
      :model-value="modelValue"
      :name="name"
      :placeholder="placeholder"
      class="min-w-0 flex-1"
      autocomplete="off"
      @update:model-value="emit('update:modelValue', String($event))"
    />
    <Button type="submit" size="sm" shape="square" :disabled="pending || !modelValue.trim()">
      {{ submitLabel }}
    </Button>
  </form>
</template>
