<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { highlightFence, renderMarkdownText, splitMarkdownSource } from '@/lib/markdownFences'
import { Eye, FilePen } from 'lucide-vue-next'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Textarea } from '@/ui/textarea'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    modelValue: string
    title?: string
    label?: string
    readonly?: boolean
    preview?: boolean
    rows?: number
    variant?: 'panel' | 'flush'
    /** Hide Preview/Edit. Flush already hides it so the parent can own Edit/Save. */
    hideModeToggle?: boolean
    tags?: string[]
  }>(),
  {
    label: '',
    readonly: false,
    preview: undefined,
    rows: 16,
    variant: 'panel',
    hideModeToggle: false,
    tags: () => [],
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'update:title': [value: string]
}>()

const copiedIndex = ref<number | null>(null)
const showTitle = computed(() => props.title !== undefined)
const modeToggleHidden = computed(() => props.variant === 'flush' || props.hideModeToggle)
const internalPreview = ref(true)
const isPreview = computed(() => props.preview ?? internalPreview.value)

watch(
  () => props.readonly,
  (readonly) => {
    if (readonly) {
      internalPreview.value = true
    }
  },
  { immediate: true },
)

function setPreview(value: boolean) {
  internalPreview.value = value
}

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
  <div :class="cn('min-w-0', variant === 'flush' ? 'flex min-h-0 flex-1 flex-col' : 'space-y-3')">
    <div :class="cn(variant === 'flush' ? 'shrink-0 border-b px-5 py-3' : 'space-y-2')">
      <div
        :class="cn('flex items-center gap-2', variant !== 'flush' && 'flex-wrap justify-between')"
      >
        <p
          v-if="showTitle && isPreview"
          class="min-w-0 flex-1 truncate text-lg font-semibold tracking-tight"
        >
          {{ title || 'Untitled' }}
        </p>
        <Input
          v-else-if="showTitle && !readonly"
          :model-value="title"
          name="title"
          aria-label="Title"
          placeholder="Untitled"
          autocomplete="off"
          class="h-auto min-w-0 flex-1 border-0 bg-transparent px-0 text-lg font-semibold tracking-tight shadow-none focus-visible:border-transparent focus-visible:ring-0"
          @update:model-value="emit('update:title', String($event))"
        />
        <p v-else-if="label" class="text-sm font-medium">{{ label }}</p>
        <div v-else class="min-w-0 flex-1" />

        <div class="ml-auto flex shrink-0 items-center gap-1.5">
          <slot name="toolbar" />
          <Button
            v-if="!modeToggleHidden && isPreview"
            type="button"
            variant="outline"
            size="sm"
            shape="square"
            @click="setPreview(false)"
          >
            <FilePen />
            Edit
          </Button>
          <Button
            v-else-if="!modeToggleHidden"
            type="button"
            size="sm"
            variant="outline"
            shape="square"
            @click="setPreview(true)"
          >
            <Eye />
            Preview
          </Button>
        </div>
      </div>
      <p v-if="tags.length" class="min-w-0 truncate pt-1 text-xs text-muted-foreground">
        {{ tags.join(' · ') }}
      </p>
    </div>

    <Textarea
      v-if="!isPreview"
      :model-value="modelValue"
      :readonly="readonly"
      :rows="rows"
      name="markdown"
      spellcheck="false"
      :class="
        variant === 'flush'
          ? 'min-h-0 flex-1 resize-none rounded-none border-0 shadow-none focus-visible:ring-0'
          : 'min-h-64'
      "
      @update:model-value="setSource"
    />

    <div
      v-else
      :class="
        cn(
          'markdown-preview min-h-0',
          variant === 'flush'
            ? 'flex-1 overflow-y-auto px-5 py-5'
            : 'space-y-4 rounded-md border bg-card p-4',
        )
      "
    >
      <div :class="variant === 'flush' ? 'space-y-4' : undefined">
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
        <p v-if="segments.length === 0" class="text-sm text-muted-foreground">
          Nothing to preview.
        </p>
      </div>
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
