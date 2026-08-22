<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { activityApi, type ActivityItem } from '@/api/activityApi'
import { issuesApi, ISSUE_STATUSES, type Issue } from '@/api/issuesApi'
import { projectsApi, type Project, type ProjectContext } from '@/api/projectsApi'
import { sessionsApi, type ProjectSession } from '@/api/sessionsApi'
import { viewsApi, type SavedView } from '@/api/viewsApi'
import { applyActivityView, applyIssueView, groupsByStatus } from '@/lib/savedViews'
import {
  Button,
  DataList,
  DataListEmpty,
  DataListItem,
  FormField,
  FormSection,
  FormSlideout,
  Input,
  MarkdownSource,
  PageBody,
  PageHeader,
  SavedViewBar,
  StatusMessage,
  WorkbenchPanes,
} from '@/ui'

const route = useRoute()
const router = useRouter()

const projects = ref<Project[]>([])
const issues = ref<Issue[]>([])
const sessions = ref<ProjectSession[]>([])
const activity = ref<ActivityItem[]>([])
const issueViews = ref<SavedView[]>([])
const activityViews = ref<SavedView[]>([])
const context = ref<ProjectContext | null>(null)
const selectedId = ref<string | null>(null)
const selectedIssue = ref<Issue | null>(null)
const selectedIssueViewId = ref<string | null>(null)
const selectedActivityViewId = ref<string | null>(null)
const issueFilterOpen = ref(false)
const activityFilterOpen = ref(false)
const issueFilterName = ref('')
const issueFilterStatus = ref('')
const issueFilterGroup = ref('')
const activityFilterName = ref('')
const activityFilterType = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const notice = ref<string | null>(null)

const projectSlideoutOpen = ref(false)
const contextSlideoutOpen = ref(false)
const issueSlideoutOpen = ref(false)
const createIssueOpen = ref(false)

const projectName = ref('')
const contextTitle = ref('')
const contextBody = ref('')
const issueTitle = ref('')
const issueDescription = ref('')

const selected = computed(
  () => projects.value.find((project) => project.id === selectedId.value) ?? null,
)

const recentSessions = computed(() => sessions.value.slice(0, 5))

const selectedIssueView = computed(
  () => issueViews.value.find((view) => view.id === selectedIssueViewId.value) ?? null,
)

const selectedActivityView = computed(
  () => activityViews.value.find((view) => view.id === selectedActivityViewId.value) ?? null,
)

const visibleIssues = computed(() => applyIssueView(issues.value, selectedIssueView.value))

const issuesByStatus = computed(() => groupsByStatus(visibleIssues.value, ISSUE_STATUSES))

const groupedIssues = computed(() => selectedIssueView.value?.groupBy === 'status')

const visibleActivity = computed(() =>
  applyActivityView(activity.value, selectedActivityView.value),
)

function actorLabel(session: ProjectSession) {
  return session.actorAiClientId ? 'AI Client' : 'Owner'
}

function sessionWhen(session: ProjectSession) {
  const started = new Date(session.startedAt).toLocaleString()
  if (!session.finishedAt) {
    return `${started} · open`
  }

  return `${started} · finished`
}

async function loadProjects() {
  loading.value = true
  error.value = null
  try {
    projects.value = await projectsApi.list()
    const requested = typeof route.params.idOrSlug === 'string' ? route.params.idOrSlug : ''
    const match =
      projects.value.find((project) => project.slug === requested || project.id === requested) ??
      projects.value[0] ??
      null
    if (match) {
      await selectProject(match, requested !== match.slug && requested !== match.id)
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load Projects'
    throw e
  } finally {
    loading.value = false
  }
}

async function selectProject(project: Project, syncRoute = true) {
  selectedId.value = project.id
  selectedIssue.value = null
  notice.value = null
  if (syncRoute && route.params.idOrSlug !== project.slug) {
    await router.replace({ name: 'projects', params: { idOrSlug: project.slug } })
  }

  try {
    const [
      issueItems,
      contextItem,
      sessionItems,
      activityItems,
      issueViewItems,
      activityViewItems,
    ] = await Promise.all([
      issuesApi.list(project.id),
      projectsApi.getContext(project.id),
      sessionsApi.list(project.id),
      activityApi.list(project.id),
      viewsApi.list('Issues', project.id),
      viewsApi.list('Activity', project.id),
    ])
    issues.value = issueItems
    context.value = contextItem
    sessions.value = sessionItems
    activity.value = activityItems
    issueViews.value = issueViewItems
    activityViews.value = activityViewItems
    if (
      !selectedIssueViewId.value ||
      !issueViewItems.some((view) => view.id === selectedIssueViewId.value)
    ) {
      selectedIssueViewId.value = issueViewItems[0]?.id ?? null
    }
    if (
      !selectedActivityViewId.value ||
      !activityViewItems.some((view) => view.id === selectedActivityViewId.value)
    ) {
      selectedActivityViewId.value = activityViewItems[0]?.id ?? null
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load the Project'
    throw e
  }
}

function openCreateProject() {
  projectName.value = ''
  projectSlideoutOpen.value = true
}

function openContext() {
  if (!context.value) {
    return
  }

  contextTitle.value = context.value.title
  contextBody.value = context.value.body
  contextSlideoutOpen.value = true
}

function openIssue(issue: Issue) {
  selectedIssue.value = issue
  issueSlideoutOpen.value = true
}

function openCreateIssue() {
  issueTitle.value = ''
  issueDescription.value = ''
  createIssueOpen.value = true
}

async function createProject() {
  if (!projectName.value.trim()) {
    error.value = 'Name is required.'
    return
  }

  saving.value = true
  error.value = null
  try {
    const created = await projectsApi.create(projectName.value.trim())
    projectSlideoutOpen.value = false
    await loadProjects()
    await selectProject(created)
    notice.value = 'Project created.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to create Project'
    throw e
  } finally {
    saving.value = false
  }
}

async function saveContext() {
  if (!selected.value || !context.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    context.value = await projectsApi.saveContext(
      selected.value.id,
      context.value.revisionId,
      contextTitle.value,
      contextBody.value,
    )
    contextSlideoutOpen.value = false
    notice.value = 'Project Context saved.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to save Project Context'
    throw e
  } finally {
    saving.value = false
  }
}

async function createIssue() {
  if (!selected.value || !issueTitle.value.trim()) {
    error.value = 'Title is required.'
    return
  }

  saving.value = true
  error.value = null
  try {
    const created = await issuesApi.create(
      selected.value.id,
      issueTitle.value.trim(),
      issueDescription.value,
    )
    issues.value = await issuesApi.list(selected.value.id)
    createIssueOpen.value = false
    openIssue(created)
    notice.value = 'Issue created.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to create Issue'
    throw e
  } finally {
    saving.value = false
  }
}

async function duplicateIssueView() {
  if (!selectedIssueViewId.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const copy = await viewsApi.duplicate(selectedIssueViewId.value, undefined, selected.value?.id)
    issueViews.value = await viewsApi.list('Issues', selected.value?.id)
    selectedIssueViewId.value = copy.id
    notice.value = 'Duplicated issue Saved View.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to duplicate Saved View'
    throw e
  } finally {
    saving.value = false
  }
}

function openIssueFilters() {
  if (!selectedIssueView.value || selectedIssueView.value.isSystem) {
    return
  }

  issueFilterName.value = selectedIssueView.value.name
  issueFilterStatus.value = selectedIssueView.value.filters.status ?? ''
  issueFilterGroup.value = selectedIssueView.value.groupBy ?? ''
  issueFilterOpen.value = true
}

async function saveIssueFilters() {
  if (!selectedIssueView.value || selectedIssueView.value.isSystem) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const updated = await viewsApi.update(selectedIssueView.value.id, {
      name: issueFilterName.value,
      filters: issueFilterStatus.value ? { status: issueFilterStatus.value } : {},
      groupBy: issueFilterGroup.value || null,
    })
    issueViews.value = await viewsApi.list('Issues', selected.value?.id)
    selectedIssueViewId.value = updated.id
    issueFilterOpen.value = false
    notice.value = 'Issue Saved View updated.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to update Saved View'
    throw e
  } finally {
    saving.value = false
  }
}

async function duplicateActivityView() {
  if (!selectedActivityViewId.value) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const copy = await viewsApi.duplicate(
      selectedActivityViewId.value,
      undefined,
      selected.value?.id,
    )
    activityViews.value = await viewsApi.list('Activity', selected.value?.id)
    selectedActivityViewId.value = copy.id
    notice.value = 'Duplicated Activity Saved View.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to duplicate Saved View'
    throw e
  } finally {
    saving.value = false
  }
}

function openActivityFilters() {
  if (!selectedActivityView.value || selectedActivityView.value.isSystem) {
    return
  }

  activityFilterName.value = selectedActivityView.value.name
  activityFilterType.value = selectedActivityView.value.filters.recordType ?? ''
  activityFilterOpen.value = true
}

async function saveActivityFilters() {
  if (!selectedActivityView.value || selectedActivityView.value.isSystem) {
    return
  }

  saving.value = true
  error.value = null
  try {
    const updated = await viewsApi.update(selectedActivityView.value.id, {
      name: activityFilterName.value,
      filters: activityFilterType.value ? { recordType: activityFilterType.value } : {},
    })
    activityViews.value = await viewsApi.list('Activity', selected.value?.id)
    selectedActivityViewId.value = updated.id
    activityFilterOpen.value = false
    notice.value = 'Activity Saved View updated.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to update Saved View'
    throw e
  } finally {
    saving.value = false
  }
}

watch(
  () => route.params.idOrSlug,
  (idOrSlug) => {
    if (typeof idOrSlug !== 'string' || !projects.value.length) {
      return
    }

    const match = projects.value.find(
      (project) => project.slug === idOrSlug || project.id === idOrSlug,
    )
    if (match && match.id !== selectedId.value) {
      void selectProject(match, false)
    }
  },
)

onMounted(() => {
  void loadProjects()
})
</script>

<template>
  <PageBody>
    <PageHeader title="Projects" description="Issues, Project Context, and recent Sessions.">
      <template #actions>
        <Button shape="square" @click="openCreateProject">New project</Button>
      </template>
    </PageHeader>

    <StatusMessage v-if="error" tone="error">{{ error }}</StatusMessage>
    <StatusMessage v-else-if="notice" tone="success">{{ notice }}</StatusMessage>

    <WorkbenchPanes
      :system-view="selectedIssueView?.name ?? 'Issues by status'"
      nav-title="Projects"
      list-title="Issues"
      detail-title="Context"
    >
      <template #nav>
        <DataList>
          <DataListEmpty v-if="!loading && projects.length === 0">No Projects yet.</DataListEmpty>
          <DataListItem
            v-for="project in projects"
            :key="project.id"
            :title="project.name"
            :description="project.slug"
            interactive
            :selected="selectedId === project.id"
            @click="selectProject(project)"
          />
        </DataList>
      </template>

      <template #list>
        <SavedViewBar
          class="mb-4"
          :views="issueViews"
          :selected-id="selectedIssueViewId"
          :pending="saving"
          @select="selectedIssueViewId = $event"
          @duplicate="duplicateIssueView"
          @edit="openIssueFilters"
        />
        <Button
          v-if="selected"
          class="mb-4"
          size="sm"
          shape="square"
          :disabled="saving"
          @click="openCreateIssue"
        >
          New issue
        </Button>
        <StatusMessage v-if="!selected">Select a Project to see its Issues.</StatusMessage>
        <template v-if="groupedIssues">
          <template v-for="bucket in issuesByStatus" :key="bucket.status">
            <h3 class="mt-4 mb-2 text-sm font-semibold">{{ bucket.status }}</h3>
            <DataList>
              <DataListEmpty v-if="bucket.items.length === 0">None</DataListEmpty>
              <DataListItem
                v-for="issue in bucket.items"
                :key="issue.id"
                :title="issue.title"
                :description="issue.effectivelyBlocked ? 'Effectively blocked' : issue.priority"
                interactive
                :selected="selectedIssue?.id === issue.id"
                @click="openIssue(issue)"
              />
            </DataList>
          </template>
        </template>
        <DataList v-else>
          <DataListEmpty v-if="!visibleIssues.length">No Issues in this view.</DataListEmpty>
          <DataListItem
            v-for="issue in visibleIssues"
            :key="issue.id"
            :title="issue.title"
            :description="issue.status"
            interactive
            :selected="selectedIssue?.id === issue.id"
            @click="openIssue(issue)"
          />
        </DataList>
      </template>

      <template #detail>
        <StatusMessage v-if="!selected"
          >Select a Project to read Context and Sessions.</StatusMessage
        >
        <template v-else>
          <Button class="mb-4" size="sm" shape="square" @click="openContext">Edit context</Button>
          <MarkdownSource
            v-if="context"
            :model-value="context.body"
            :label="context.title"
            readonly
          />
          <h3 class="mt-6 mb-2 font-semibold">Recent Sessions</h3>
          <DataList>
            <DataListEmpty v-if="recentSessions.length === 0">No Sessions yet.</DataListEmpty>
            <DataListItem
              v-for="session in recentSessions"
              :key="session.id"
              :title="session.summary ?? 'Session'"
              :description="`${actorLabel(session)} · ${sessionWhen(session)}`"
            />
          </DataList>
          <h3 class="mt-6 mb-2 font-semibold">Activity</h3>
          <SavedViewBar
            class="mb-4"
            :views="activityViews"
            :selected-id="selectedActivityViewId"
            :pending="saving"
            @select="selectedActivityViewId = $event"
            @duplicate="duplicateActivityView"
            @edit="openActivityFilters"
          />
          <DataList>
            <DataListEmpty v-if="visibleActivity.length === 0"
              >No Activity in this view.</DataListEmpty
            >
            <DataListItem
              v-for="item in visibleActivity"
              :key="item.id"
              :title="item.summary"
              :description="`${item.recordType} · ${item.occurredAt}`"
            />
          </DataList>
        </template>
      </template>
    </WorkbenchPanes>

    <FormSlideout
      :open="projectSlideoutOpen"
      title="New Project"
      description="Creates an ordinary Project with its own Context."
      submit-label="Create"
      :pending="saving"
      @update:open="projectSlideoutOpen = $event"
      @submit="createProject"
    >
      <FormSection title="Project">
        <FormField label="Name" required>
          <Input v-model="projectName" name="project-name" autocomplete="off" />
        </FormField>
      </FormSection>
    </FormSlideout>

    <FormSlideout
      :open="contextSlideoutOpen"
      title="Project Context"
      description="Source Markdown is stored exactly. Preview, highlight, and copy do not change it."
      submit-label="Save"
      :pending="saving"
      allow-fullscreen
      size="wide"
      @update:open="contextSlideoutOpen = $event"
      @submit="saveContext"
    >
      <FormSection title="Context">
        <FormField label="Title">
          <Input v-model="contextTitle" name="context-title" autocomplete="off" />
        </FormField>
        <MarkdownSource v-model="contextBody" />
      </FormSection>
    </FormSlideout>

    <FormSlideout
      :open="issueSlideoutOpen"
      :title="selectedIssue?.title ?? 'Issue'"
      :description="
        selectedIssue ? `${selectedIssue.status} · ${selectedIssue.priority}` : undefined
      "
      :show-submit="false"
      cancel-label="Close"
      allow-fullscreen
      size="wide"
      @update:open="issueSlideoutOpen = $event"
    >
      <FormSection v-if="selectedIssue" title="Issue">
        <StatusMessage v-if="selectedIssue.blockedReason">
          Blocked: {{ selectedIssue.blockedReason }}
        </StatusMessage>
        <StatusMessage v-if="selectedIssue.resolution">
          Resolution: {{ selectedIssue.resolution }}
        </StatusMessage>
        <MarkdownSource
          :model-value="selectedIssue.description ?? ''"
          label="Description"
          readonly
        />
      </FormSection>
    </FormSlideout>

    <FormSlideout
      :open="createIssueOpen"
      title="New Issue"
      description="Creates a Backlog Issue on the selected Project."
      submit-label="Create"
      :pending="saving"
      allow-fullscreen
      size="wide"
      @update:open="createIssueOpen = $event"
      @submit="createIssue"
    >
      <FormSection title="Issue">
        <FormField label="Title" required>
          <Input v-model="issueTitle" name="issue-title" autocomplete="off" />
        </FormField>
        <MarkdownSource v-model="issueDescription" label="Description" />
      </FormSection>
    </FormSlideout>

    <FormSlideout
      :open="issueFilterOpen"
      title="Edit Issue Saved View"
      description="Changes apply to this copy. The system original stays read-only."
      submit-label="Save"
      :pending="saving"
      @update:open="issueFilterOpen = $event"
      @submit="saveIssueFilters"
    >
      <FormSection title="Filters">
        <FormField label="Name" required>
          <Input v-model="issueFilterName" name="issue-view-name" autocomplete="off" />
        </FormField>
        <FormField label="Status">
          <select v-model="issueFilterStatus">
            <option value="">Any</option>
            <option v-for="status in ISSUE_STATUSES" :key="status" :value="status">
              {{ status }}
            </option>
          </select>
        </FormField>
        <FormField label="Group by">
          <select v-model="issueFilterGroup">
            <option value="">None</option>
            <option value="status">Status</option>
          </select>
        </FormField>
      </FormSection>
    </FormSlideout>

    <FormSlideout
      :open="activityFilterOpen"
      title="Edit Activity Saved View"
      description="Changes apply to this copy. The system original stays read-only."
      submit-label="Save"
      :pending="saving"
      @update:open="activityFilterOpen = $event"
      @submit="saveActivityFilters"
    >
      <FormSection title="Filters">
        <FormField label="Name" required>
          <Input v-model="activityFilterName" name="activity-view-name" autocomplete="off" />
        </FormField>
        <FormField label="Record type">
          <select v-model="activityFilterType">
            <option value="">Any</option>
            <option value="Issue">Issue</option>
            <option value="Document">Document</option>
            <option value="Session">Session</option>
            <option value="InboxItem">Inbox Item</option>
            <option value="Project">Project</option>
          </select>
        </FormField>
      </FormSection>
    </FormSlideout>
  </PageBody>
</template>
