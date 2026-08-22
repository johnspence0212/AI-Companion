<script setup lang="ts">
/**
 * PROTOTYPE only for JOH-22.
 * Three variants of the V1 workbench, switchable via ?variant=, on /prototype/workbench.
 */
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { StatusMessage } from '@/ui'
import PrototypeSwitcher from './PrototypeSwitcher.vue'
import VariantToday from './VariantToday.vue'
import VariantLibrary from './VariantLibrary.vue'
import VariantExecution from './VariantExecution.vue'
import { prototypeNote } from './workbench-data'

const route = useRoute()
const variant = computed(() => String(route.query.variant ?? 'A'))
</script>

<template>
  <div>
    <div class="px-8 pt-6">
      <StatusMessage>
        PROTOTYPE. {{ prototypeNote }} In-memory fixtures only. Do not treat this as production UI.
      </StatusMessage>
    </div>
    <VariantToday v-if="variant === 'A'" />
    <VariantLibrary v-else-if="variant === 'B'" />
    <VariantExecution v-else />
    <PrototypeSwitcher />
  </div>
</template>
