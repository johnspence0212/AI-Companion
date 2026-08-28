# AI Companion

AI Companion is a personal project memory and execution system shared by a human and AI
clients.

## Language

**Project**:
The execution boundary for a coherent body of work, including its Project Context,
Issues, Sessions, and Activity. It may link to independent Documents without owning them.
_Avoid_: Workspace, Linear project

**Project Context**:
The one Project-owned special Document that orients a human or AI client to the
Project's goal, current state, decisions, constraints, and priorities. It sits outside
the independent Document library and cannot link to another Project.
_Avoid_: Project summary, dashboard

**Document**:
Durable Markdown knowledge in the user's independent library, including essays, notes,
research, journals, and other authored material. A Document may support any number of
Projects without being owned by them; its content history is an immutable sequence of
revisions. The Document points at exactly one current revision. A Document may contain
other Documents. Nesting a Document under another Document is the Library folder
structure; the parent still has its own Markdown body. Library copy may say "note".
_Avoid_: page, wiki

**Revision**:
An immutable snapshot of a Document's title and full Markdown body at an accepted write,
including save, append, or restore. Restoring older content creates a new Revision.
_Avoid_: Diff, autosave snapshot

**Folder**:
An explicit nested container still available to the API and MCP. The Library destination
organizes by nested Documents instead of Folders. A regular Document may still belong to
at most one Folder.
_Avoid_: Project, label

**Tag**:
A user-global classification shared by Documents and Issues.
_Avoid_: Folder, Project

**Saved View**:
A reusable table or list configuration targeting one first-class entity type, including
its visible columns, filters, sorting, and at most one grouping field. It may be
user-global or scoped to one Project. System defaults are read-only and may be duplicated.
_Avoid_: Custom database, arbitrary schema, board

**Document Template**:
A user-owned named starter that copies a title pattern and initial Markdown into a new
Document. The new Document does not stay linked to the template.
_Avoid_: Live template, kind field

**Issue**:
The universal tracked unit of work belonging to exactly one Project. Labels distinguish
executable work, bugs, risks, decisions, maps, and other purposes.
_Avoid_: Action, Task, Ticket

**Ready Issue**:
An Issue declared actionable but not yet started. An unresolved dependency may still
make it effectively blocked.
_Avoid_: Todo, queued task

**Claim**:
The exclusive assignment of a nonterminal Issue to one human or AI Client. A Claim
persists until explicitly released, reassigned, or superseded by terminal status.
_Avoid_: Lease, lock

**Start**:
The explicit operation that assigns an unassigned Ready Issue to the caller and moves it
to Active. Reading available work never starts it implicitly.
_Avoid_: Claim, view

**Blocker**:
Either an unresolved Issue dependency or an explicit impediment with a Markdown reason.
_Avoid_: Pause, hold

**Resolution**:
The attributed Markdown comment written atomically when an Issue becomes Done.
_Avoid_: Completion note

**Today**:
The signed-in home. It shows one Owner's Daily list for the user-local date, a 7-day
carryover group that does not auto-move items, and derived Blocked/Waiting Issues.
_Avoid_: Dashboard, inbox, calendar

**Bootstrap Project**:
The empty Project and Project Context created on an Owner's first login so the
workspace is not blank. It is ordinary Project data, not demo seed.
_Avoid_: Sample data, onboarding tour

**Workbench**:
The product shell. The rail destinations are Today, Projects, and Library.
Search is a centered field in the top bar; submitting it opens a centered overlay with
grouped results and greys out the destination behind it. Inbox is a top-bar button next
to Search and opens the same kind of overlay for capture and processing.
Projects is three-pane. Library is a notes tree plus a reading pane: Preview is the default
surface, Edit opens Source and becomes Save, and adding a note to a note nests it.
Issue, Project Context, Inbox, and Search Document hits open in
a right slide-out with optional fullscreen. Saved Views are the list mode inside Projects, not a separate home.
_Avoid_: Command palette, Views home, chat shell, Search or Inbox rail destination

**Daily Item**:
An ordered item on one Owner's user-local date. It is either a single Issue reference or
custom text, never both. Completing it does not complete the Issue. Incomplete items stay
on their original date.
_Avoid_: Calendar event

**Inbox Item**:
An unclassified thought captured before deciding whether it should become a Document,
Issue, or other Project information. Processing creates or attaches a target and keeps a
provenance link. Archiving dismisses without conversion.
_Avoid_: Issue, Document

**Session**:
An explicitly started, bounded period of human or AI work within one Project. At most one
open Session exists per actor per Project. Touched records and the finish summary come
from Activity, not a transcript.
_Avoid_: Full transcript, implicit session

**Activity**:
An immutable, attributed record that a meaningful domain change occurred. It identifies
the affected record and may provide Project and Session context.
_Avoid_: Security audit event

**Owner**:
The authenticated human user who privately owns a record. Every product operation is
limited to that user's data. Platform administrators do not bypass this boundary.
_Avoid_: Tenant, organization

**AI Client**:
A user-owned credentialed integration that acts on the Owner's data and is recorded
alongside the Owner on every mutation it performs. Local V1 authenticates MCP with a
bearer secret shown once and stored as a hash.
_Avoid_: User, generic AI actor, service account, browser cookie

**MCP Adapter**:
The thin Streamable HTTP `/mcp` surface that exposes tools and resources by calling the
same application services as the browser API.
_Avoid_: Separate domain layer, stdio host

**Command**:
A named user operation. A searchable palette, mnemonic chords, and Super-key desktop
bridge are out of V1.
_Avoid_: Shortcut, menu item

**Archive**:
The reversible removal of a mutable record from active use. Archiving never destroys
content; Activity and revisions are immutable and never archived.
_Avoid_: Delete, purge
