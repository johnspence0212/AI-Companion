<script setup lang="ts">
/** PROTOTYPE variant C: Project execution three-pane. Documents appear as linked context. */
import { PageBody, PageHeader, SurfaceCard } from '@/ui'
import { issues } from './workbench-data'

const active = issues.filter((issue) => issue.status === 'Active')
const ready = issues.filter((issue) => issue.status === 'Ready')
</script>

<template>
  <PageBody>
    <PageHeader
      title="Fantasy Football"
      description="Execution workbench. Context, Issues, and recent Sessions stay in one Project. Documents are linked, not owned."
    />

    <div class="grid gap-4 xl:grid-cols-[14rem_22rem_minmax(0,1fr)]">
      <SurfaceCard>
        <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Projects</p>
        <ul class="mt-3 space-y-2 text-sm">
          <li class="font-semibold">Fantasy Football</li>
          <li>AI Companion</li>
          <li>Enterprise Template</li>
        </ul>
      </SurfaceCard>

      <SurfaceCard>
        <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Active</p>
        <ul class="mt-2 space-y-2 text-sm">
          <li v-for="issue in active" :key="issue.id">{{ issue.title }}</li>
        </ul>
        <p class="mt-4 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Ready</p>
        <ul class="mt-2 space-y-2 text-sm">
          <li v-for="issue in ready" :key="issue.id">{{ issue.title }}</li>
        </ul>
      </SurfaceCard>

      <div class="space-y-4">
        <SurfaceCard>
          <h2 class="font-semibold">Project Context</h2>
          <p class="mt-2 text-sm text-muted-foreground">Goal · Current State · Current Priorities</p>
          <p class="mt-3 text-sm">Prove Cursor can read context, start a Ready Issue, write a Document, and complete the Issue.</p>
        </SurfaceCard>
        <SurfaceCard>
          <h2 class="font-semibold">Implement ESPN roster sync</h2>
          <p class="mt-2 text-sm">Status Active · assigned Cursor</p>
          <p class="mt-3 text-sm text-muted-foreground">Linked Documents: ESPN token lifecycle</p>
        </SurfaceCard>
        <SurfaceCard>
          <h2 class="font-semibold">Recent Sessions</h2>
          <p class="mt-2 text-sm">Today · Cursor · started roster sync, appended token note</p>
        </SurfaceCard>
      </div>
    </div>
  </PageBody>
</template>
