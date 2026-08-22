<script setup lang="ts">
import { computed, ref } from 'vue'
import { highlightFence, renderMarkdownText, splitMarkdownSource } from '@/lib/markdownFences'
import { Button } from '@/ui/button'
import { Textarea } from '@/ui/textarea'

const props = withDefaults(
  defineProps<{
    modelValue: string
    label?: string
    readonly?: boolean
    rows?: number
  }>(),
  {
    label: 'Markdown',
    readonly: false,
    rows: 16,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const preview = ref(false)
const copiedIndex = ref<number | null>(null)

const segments = computed(() =>
  splitMarkdownSource(props.modelValue).map((segment, index) => {
    if (segment.type === 'text') {
      return {
        key: `text-${index}`,
        type: 'text' as const,
        html: renderMarkdownText(segment.text),
      }
    }

    return {
      key: `fence-${index}`,
      type: 'fence' as const,
      language: segment.language || 'plain',
      code: segment.code,
      html: highlightFence(segment.language, segment.code),
    }
  }),
)

function setSource(value: string) {
  if (!props.readonly) {
    emit('update:modelValue', value)
  }
}

async function copyFence(code: string, index: number) {
  await navigator.clipboard.writeText(code)
  copiedIndex.value = index
}
</script>

<template>
  <div class="space-y-3">
    <div class="flex flex-wrap items-center justify-between gap-2">
      <p class="text-sm font-medium">{{ label }}</p>
      <Button type="button" size="sm" variant="outline" shape="square" @click="preview = !preview">
        {{ preview ? 'Source' : 'Preview' }}
      </Button>
    </div>

    <Textarea
      v-if="!preview"
      :model-value="modelValue"
      :readonly="readonly"
      :rows="rows"
      name="markdown"
      spellcheck="false"
      class="min-h-64"
      @update:model-value="setSource"
    />

    <div v-else class="markdown-preview space-y-4 rounded-md border bg-card p-4">
      <template v-for="(segment, index) in segments" :key="segment.key">
        <div v-if="segment.type === 'text'" class="markdown-text" v-html="segment.html" />
        <div v-else class="overflow-hidden rounded-md border">
          <div class="flex items-center justify-between gap-2 border-b bg-muted px-3 py-2">
            <p class="text-xs font-medium tracking-wide text-muted-foreground uppercase">
              {{ segment.language }}
            </p>
            <Button
              type="button"
              size="sm"
              variant="outline"
              shape="square"
              @click="copyFence(segment.code, index)"
            >
              {{ copiedIndex === index ? 'Copied' : 'Copy' }}
            </Button>
          </div>
          <pre
            class="overflow-x-auto p-3 text-sm"
          ><code class="markdown-code" v-html="segment.html" /></pre>
        </div>
      </template>
      <p v-if="segments.length === 0" class="text-sm text-muted-foreground">Nothing to preview.</p>
    </div>
  </div>
</template>

<style scoped>
.markdown-text :deep(h1),
.markdown-text :deep(h2),
.markdown-text :deep(h3) {
  font-weight: 600;
  margin-bottom: 0.5rem;
}

.markdown-text :deep(h1) {
  font-size: 1.25rem;
}

.markdown-text :deep(h2) {
  font-size: 1.1rem;
}

.markdown-text :deep(p) {
  margin-bottom: 0.75rem;
}

.markdown-text :deep(code) {
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.875em;
}

.markdown-code :deep(.hljs-keyword),
.markdown-code :deep(.hljs-selector-tag),
.markdown-code :deep(.hljs-literal) {
  color: var(--primary);
}

.markdown-code :deep(.hljs-string),
.markdown-code :deep(.hljs-attr) {
  color: var(--brand-muted);
}

.markdown-code :deep(.hljs-comment) {
  color: var(--muted-foreground);
}

.markdown-code :deep(.hljs-number),
.markdown-code :deep(.hljs-title),
.markdown-code :deep(.hljs-type) {
  color: var(--foreground);
}
</style>
