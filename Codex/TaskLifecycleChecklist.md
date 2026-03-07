# Codex Task Lifecycle Checklist

Use this checklist at the start and end of every task.

## Required Workflow
- [ ] Create a GitHub issue for the task before implementation starts.
- [ ] Add label `inprogress` when work begins.
- [ ] Add a start comment with scope and planned deliverables.
- [ ] Post progress comments during substantial work.
- [ ] Run validation (build/tests and relevant smoke checks).
- [ ] Add a changelog file in `changelog/YYYY-MM-DD-issue-<id>-*.md`.
- [ ] Commit and push code changes.
- [ ] Add completion comment with:
  - delivered changes
  - validation commands/results
  - commit hash
  - time spent
- [ ] Remove label `inprogress`.
- [ ] Close the GitHub issue.

## Session Run Log
- Active task: `#236 Family member bulk import (CSV) with preview and validation`.
- Phase 2 source of truth: `Codex/phase2codex.md`.
- Previous completed task: `#240` Role-aware command gating audit across desktop workspaces.
- Last updated: `2026-03-07`.
