<script setup lang="ts">
/** PROTOTYPE variant A: Today is home. Click a destination to walk the rest of the IA. */
import { computed, ref } from 'vue'
import {
  Button,
  DataList,
  DataListItem,
  FormSlideout,
  PageBody,
  PageHeader,
  StatusMessage,
  SurfaceCard,
} from '@/ui'
import {
  daily,
  documents,
  inboxItems,
  issues,
  markdownSample,
  navItems,
  projects,
  type NavId,
} from './workbench-data'

const screen = ref<NavId>('today')
const selectedProject = ref(projects[0]!)
const selectedDocumentId = ref(documents[0]!.id)
const detailOpen = ref(false)
const detailTitle = ref('Issue')
const detailKind = ref<'issue' | 'document' | 'inbox'>('issue')

const blocked = computed(() => issues.filter((issue) => issue.status === 'Blocked'))
const projectIssues = computed(() => issues.filter((issue) => issue.project === selectedProject.value))
const selectedDocument = computed(
  () => documents.find((document) => document.id === selectedDocumentId.value) ?? documents[0]!,
)

function openIssue(title: string) {
  detailKind.value = 'issue'
  detailTitle.value = title
  detailOpen.value = true
}

function openDocument(id: string) {
  selectedDocumentId.value = id
  detailKind.value = 'document'
  detailTitle.value = documents.find((document) => document.id === id)?.title ?? 'Document'
  detailOpen.value = true
}

function openInbox(title: string) {
  detailKind.value = 'inbox'
  detailTitle.value = title
  detailOpen.value = true
}
</script>

<template>
  <PageBody>
    <PageHeader
      title="Today-first workbench"
      description="Home is Daily. Projects, Library, Inbox, and Search are destinations from this rail."
    />

    <div class="grid gap-4 lg:grid-cols-[14rem_minmax(0,1fr)]">
      <SurfaceCard>
        <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Navigate</p>
        <ul class="mt-3 space-y-1">
          <li v-for="item in navItems" :key="item.id">
            <Button
              class="w-full justify-start"
              :variant="screen === item.id ? 'secondary' : 'ghost'"
              @click="screen = item.id"
            >
              {{ item.label }}
              <span v-if="item.id === 'inbox'" class="ml-auto text-xs text-muted-foreground">
                {{ inboxItems.length }}
              </span>
            </Button>
          </li>
        </ul>
      </SurfaceCard>

      <div v-if="screen === 'today'" class="space-y-4">
        <SurfaceCard>
          <h2 class="font-semibold">Today · Saturday, August 22</h2>
          <DataList class="mt-3">
            <DataListItem
              v-for="item in daily"
              :key="item.id"
              :title="item.text"
              :description="item.project ? `${item.kind} · ${item.project}` : 'custom Daily Item'"
            >
              <template #actions>
                <Button
                  v-if="item.kind === 'issue'"
                  size="sm"
                  variant="outline"
                  @click="openIssue(item.text)"
                >
                  Open
                </Button>
              </template>
            </DataListItem>
          </DataList>
        </SurfaceCard>

        <SurfaceCard>
          <h2 class="font-semibold">Carryover</h2>
          <StatusMessage>
            Incomplete items from the last 7 days stay on their original dates. They are not auto-moved here.
          </StatusMessage>
        </SurfaceCard>

        <SurfaceCard>
          <h2 class="font-semibold">Blocked / Waiting</h2>
          <DataList class="mt-3">
            <DataListItem
              v-for="issue in blocked"
              :key="issue.id"
              :title="issue.title"
              :description="issue.project"
            >
              <template #actions>
                <Button size="sm" variant="outline" @click="openIssue(issue.title)">Open</Button>
              </template>
            </DataListItem>
          </DataList>
        </SurfaceCard>
      </div>

      <div v-else-if="screen === 'inbox'" class="space-y-4">
        <SurfaceCard>
          <h2 class="font-semibold">Inbox</h2>
          <StatusMessage>Manual process only. Create or attach a Document or Issue; no AI classification.</StatusMessage>
          <DataList class="mt-3">
            <DataListItem
              v-for="item in inboxItems"
              :key="item.id"
              :title="item.text"
              :description="item.source"
            >
              <template #actions>
                <Button size="sm" variant="outline" @click="openInbox(item.text)">Process</Button>
              </template>
            </DataListItem>
          </DataList>
        </SurfaceCard>
      </div>

      <div v-else-if="screen === 'projects'" class="grid gap-4 xl:grid-cols-[14rem_18rem_minmax(0,1fr)]">
        <SurfaceCard>
          <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Projects</p>
          <ul class="mt-3 space-y-1">
            <li v-for="project in projects" :key="project">
              <Button
                class="w-full justify-start"
                :variant="selectedProject === project ? 'secondary' : 'ghost'"
                @click="selectedProject = project"
              >
                {{ project }}
              </Button>
            </li>
          </ul>
        </SurfaceCard>
        <SurfaceCard>
          <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Issues</p>
          <DataList class="mt-3">
            <DataListItem
              v-for="issue in projectIssues"
              :key="issue.id"
              :title="issue.title"
              :description="`${issue.status}${issue.assignee ? ` · ${issue.assignee}` : ''}`"
            >
              <template #actions>
                <Button size="sm" variant="outline" @click="openIssue(issue.title)">Open</Button>
              </template>
            </DataListItem>
          </DataList>
        </SurfaceCard>
        <div class="space-y-4">
          <SurfaceCard>
            <h2 class="font-semibold">Project Context</h2>
            <p class="mt-2 text-sm">Goal · Current State · Current Priorities</p>
            <p class="mt-3 text-sm text-muted-foreground">
              Prove Cursor can read context, start a Ready Issue, write a Document, and complete the Issue.
            </p>
          </SurfaceCard>
          <SurfaceCard>
            <h2 class="font-semibold">Recent Sessions</h2>
            <p class="mt-2 text-sm">Today · Cursor · started roster sync, appended token note</p>
          </SurfaceCard>
        </div>
      </div>

      <div v-else-if="screen === 'library'" class="grid gap-4 xl:grid-cols-[14rem_18rem_minmax(0,1fr)]">
        <SurfaceCard>
          <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Folders</p>
          <ul class="mt-3 space-y-2 text-sm">
            <li>All Documents</li>
            <li class="font-semibold">Research</li>
            <li>Unfiled</li>
          </ul>
        </SurfaceCard>
        <SurfaceCard>
          <p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Documents</p>
          <ul class="mt-3 space-y-2">
            <li v-for="document in documents" :key="document.id">
              <Button
                class="h-auto w-full flex-col items-start justify-start py-2"
                :variant="selectedDocument.id === document.id ? 'secondary' : 'ghost'"
                @click="openDocument(document.id)"
              >
                <span>{{ document.title }}</span>
                <span class="text-xs font-normal text-muted-foreground">
                  {{ document.folder ?? 'Unfiled' }} · {{ document.updated }}
                </span>
              </Button>
            </li>
          </ul>
        </SurfaceCard>
        <SurfaceCard>
          <div class="flex items-center justify-between gap-3">
            <h2 class="font-semibold">{{ selectedDocument.title }}</h2>
            <p class="text-xs text-muted-foreground">Linked: {{ selectedDocument.project }}</p>
          </div>
          <pre class="mt-4 overflow-auto rounded-md bg-muted p-4 text-xs leading-5">{{
            markdownSample
          }}</pre>
          <Button class="mt-3" size="sm" variant="outline" @click="openDocument(selectedDocument.id)">
            Edit Markdown
          </Button>
        </SurfaceCard>
      </div>

      <SurfaceCard v-else>
        <h2 class="font-semibold">Search</h2>
        <StatusMessage>Grouped full-text over Projects, Documents, Issues, and Activity. Title then recency.</StatusMessage>
        <DataList class="mt-3">
          <DataListItem title="ESPN token lifecycle" description="Document · Research" />
          <DataListItem title="Implement ESPN roster sync" description="Issue · Fantasy Football · Active" />
          <DataListItem title="Cursor appended token note" description="Activity · Today" />
        </DataList>
      </SurfaceCard>
    </div>

    <FormSlideout
      :open="detailOpen"
      :title="detailTitle"
      :description="
        detailKind === 'document'
          ? 'Current Revision. Fenced code blocks must survive save exactly.'
          : detailKind === 'inbox'
            ? 'Process into a Document or Issue, or archive.'
            : 'Issue detail. Completing Today does not complete the Issue.'
      "
      :submit-label="detailKind === 'inbox' ? 'Attach as Issue' : 'Save'"
      allow-fullscreen
      @update:open="detailOpen = $event"
    >
      <pre
        v-if="detailKind !== 'inbox'"
        class="overflow-auto rounded-md bg-muted p-4 text-xs leading-5"
      >{{ markdownSample }}</pre>
      <StatusMessage v-else>Create Document, attach to existing Issue, or dismiss to Archived.</StatusMessage>
    </FormSlideout>
  </PageBody>
</template>
