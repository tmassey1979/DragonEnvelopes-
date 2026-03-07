# Codex Task Lifecycle Checklist

Use this checklist at the start and end of every task.
This workflow is mandatory for both `feature` and `bug` issues.

## Required Workflow
- [ ] Create a GitHub issue for the task before implementation starts.
- [ ] Use the correct label set:
  - [ ] Features: `feature`, `task`
  - [ ] Bugs: `bug`, `task`
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

## Story Format
- [ ] Write the story in user-value format: `As <x>, I would like <y>, so that <z>.`
- [ ] Include acceptance criteria and dev notes.

## Session Run Log
- Active task: update before starting work.
- Phase 2 source of truth: `Codex/phase2codex.md`.
- Previous completed task: update when closing work.
- Last updated: `2026-03-07`.
