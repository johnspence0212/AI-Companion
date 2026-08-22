# AI Companion

AI Companion is a personal project memory and execution system shared by a human and AI
clients.

## Language

**Project**:
The context boundary for a coherent body of work, including its knowledge, Issues, and
history.
_Avoid_: Workspace, Linear project

**Project Context**:
The concise, high-signal Markdown document that orients a human or AI client to a
Project's goal, current state, decisions, constraints, and priorities.
_Avoid_: Project summary, dashboard

**Note**:
Durable Markdown knowledge belonging to a Project.
_Avoid_: Document, page

**Issue**:
The universal tracked unit of work within a Project. Labels distinguish executable work,
bugs, risks, decisions, maps, and other purposes.
_Avoid_: Action, Task, Ticket

**Daily Item**:
An ordered item selected for attention on a particular date, optionally linked to an
Issue.
_Avoid_: Calendar event

**Inbox Item**:
An unclassified thought captured before deciding whether it should become a Note, Issue,
or other Project information.
_Avoid_: Issue, Note

**Session**:
An attributed, bounded period of human or AI work that groups the records touched and
the resulting summary.
_Avoid_: Full transcript

**Activity**:
An immutable, attributed record that a meaningful domain change occurred.
_Avoid_: Security audit event

**AI Client**:
An authenticated AI-facing integration that acts for a user and receives distinct
attribution for its changes.
_Avoid_: User, generic AI actor

**Command**:
A named user operation exposed through the searchable palette and keyboard grammar as
well as any relevant visual control.
_Avoid_: Shortcut, menu item
