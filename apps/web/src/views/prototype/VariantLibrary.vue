<script setup lang="ts">
/** PROTOTYPE variant B: Document library is the primary surface. Projects are links. */
import { PageBody, PageHeader, SurfaceCard } from '@/ui'
import { documents, markdownSample } from './workbench-data'
</script>

<template>
  <PageBody>
    <PageHeader
      title="Library"
      description="Independent knowledge first. Link a Document into one or more Projects when it starts supporting work."
    />

    <div class="grid gap-4 lg:grid-cols-[14rem_18rem_minmax(0,1fr)]">
      <SurfaceCard>
        <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Folders</p>
        <ul class="mt-3 space-y-2 text-sm">
          <li>All Documents</li>
          <li class="font-semibold">Research</li>
          <li class="pl-3">Essays</li>
          <li>Unfiled</li>
        </ul>
      </SurfaceCard>

      <SurfaceCard>
        <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Documents</p>
        <ul class="mt-3 space-y-3 text-sm">
          <li v-for="document in documents" :key="document.id">
            <p class="font-medium">{{ document.title }}</p>
            <p class="text-muted-foreground">
              {{ document.folder ?? 'Unfiled' }} · {{ document.updated }}
            </p>
          </li>
        </ul>
      </SurfaceCard>

      <SurfaceCard>
        <div class="flex items-center justify-between gap-3">
          <h2 class="font-semibold">ESPN token lifecycle</h2>
          <p class="text-xs text-muted-foreground">Linked: Fantasy Football</p>
        </div>
        <pre class="mt-4 overflow-auto rounded-md bg-muted p-4 text-xs leading-5">{{
          markdownSample
        }}</pre>
      </SurfaceCard>
    </div>
  </PageBody>
</template>
