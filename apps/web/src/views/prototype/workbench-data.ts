/** PROTOTYPE only. In-memory fixture for JOH-22 workbench IA. Wipe with the route. */

export const prototypeNote =
  'Three variants of the V1 workbench, switchable via ?variant= on /prototype/workbench.'

export type NavId = 'today' | 'inbox' | 'projects' | 'library' | 'search'

export const navItems: Array<{ id: NavId; label: string }> = [
  { id: 'today', label: 'Today' },
  { id: 'inbox', label: 'Inbox' },
  { id: 'projects', label: 'Projects' },
  { id: 'library', label: 'Library' },
  { id: 'search', label: 'Search' },
]

export const documents = [
  {
    id: 'doc-espn',
    title: 'ESPN token lifecycle',
    folder: 'Research',
    project: 'Fantasy Football',
    updated: 'Today',
  },
  {
    id: 'doc-mcp',
    title: 'MCP Architecture',
    folder: 'Research',
    project: 'AI Companion',
    updated: 'Yesterday',
  },
  {
    id: 'doc-essay',
    title: 'Why Issues replaced Actions',
    folder: null,
    project: 'AI Companion',
    updated: 'Mon',
  },
]

export const issues = [
  {
    id: 'iss-1',
    title: 'Implement ESPN roster sync',
    status: 'Active',
    project: 'Fantasy Football',
    assignee: 'Cursor',
  },
  {
    id: 'iss-2',
    title: 'Add waiver recommendation endpoint',
    status: 'Ready',
    project: 'Fantasy Football',
    assignee: null,
  },
  {
    id: 'iss-3',
    title: 'Document MCP Action tools',
    status: 'Ready',
    project: 'AI Companion',
    assignee: null,
  },
  {
    id: 'iss-4',
    title: 'Kalshi API access',
    status: 'Blocked',
    project: 'Fantasy Football',
    assignee: 'John',
  },
]

export const daily = [
  { id: 'd-1', text: 'Finish ESPN league importer', project: 'Fantasy Football', kind: 'issue', issueId: 'iss-1' },
  { id: 'd-2', text: 'Implement MCP Issue tools', project: 'AI Companion', kind: 'issue', issueId: 'iss-3' },
  { id: 'd-3', text: 'Buy more coffee', project: null, kind: 'custom', issueId: null },
]

export const inboxItems = [
  { id: 'in-1', text: 'Cursor noted ESPN refresh failed twice', source: 'Activity' },
  { id: 'in-2', text: 'Paste: Kalshi waitlist email', source: 'Manual' },
  { id: 'in-3', text: 'Draft: why Daily is not a board', source: 'Manual' },
]

export const projects = ['Fantasy Football', 'AI Companion', 'Enterprise Template']

export const markdownSample = `# ESPN token lifecycle

Refresh tokens expire unexpectedly.

\`\`\`ts
async function refresh(token: string) {
  return fetch('/espn/oauth/refresh', {
    method: 'POST',
    body: JSON.stringify({ token }),
  })
}
\`\`\`
`