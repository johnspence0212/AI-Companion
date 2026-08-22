# Issue Tracker

Issues for this repository live in Linear:

- Workspace: `johnspence`
- Team: `Johnspence` (`JOH`)
- Project: `AI-Companion`
- Project URL: https://linear.app/johnspence/project/ai-companion-164898102406

Use the connected Linear integration to read, create, and update issues. New issues
created by engineering skills must belong to the Johnspence team and AI-Companion
project. GitHub Issues and local markdown files are not the canonical tracker.

## Wayfinding operations

- Create the map and every decision ticket as a Linear issue in the `AI-Companion`
  project.
- Apply `wayfinder:map` to the map. Apply exactly one of `wayfinder:research`,
  `wayfinder:prototype`, `wayfinder:grilling`, or `wayfinder:task` to each ticket.
- Set each ticket's native Linear parent to the map.
- Express prerequisites with Linear's native `blockedBy` and `blocks` relationships.
- The frontier is the map's open, unassigned child issues whose blockers are all closed.
- Claim a frontier ticket by assigning it before doing any work.
- Record the answer in a resolution comment, close the ticket, then add a named link and
  one-line gist to the map's `Decisions so far`.

## Maps

- Spec (done): [Specify AI Companion V1 as a Linear replacement](https://linear.app/johnspence/issue/JOH-19/specify-ai-companion-v1-as-a-linear-replacement)
- Implementation (active): [Build AI Companion V1 through the golden path](https://linear.app/johnspence/issue/JOH-33/build-ai-companion-v1-through-the-golden-path)

The implementation map **carries execution**. Its children are build slices. Product
decisions stay on the spec map and in `CONTEXT.md`.
