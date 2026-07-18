**Write Commit Message**

Generate a commit message for the currently staged changes.

**Steps**

1. Run `!git diff --cached` to get the staged diff.
2. Analyze only what changed. If the diff is unclear, read only the surrounding function for context.

**Format**

Follow Conventional Commits: `type(scope): short description`

* Max 72 characters for header; wrap body lines at 72.
* Imperative mood (`add`, `fix`, `remove`, not `added`, `fixed`). No trailing period.

**Types** (lowercase only):
`feat`, `fix`, `perf`, `refactor`, `test`, `build`, `ci`, `docs`, `style`, `chore`, `revert`

Prefer `refactor` over `chore` when code structure changed. Never use `wip`.

**Scope** (required):
Short `kebab-case` area name, not filenames. Prefer product/system areas. Check `git log --oneline -50` to reuse existing scopes.
Examples: `save`, `ui`, `netcode`, `input`, `build`, `switch`, `localization`.

**Breaking changes**: add `!` after scope (`feat(save)!: ...`) and a `BREAKING CHANGE:` line in the body.

**Body**

Include a body when: behavior changes, bug fix (explain cause), perf change (what got faster), risky change (threading/serialization/networking/platform), workaround, or the "why" isn't obvious. Skip for purely mechanical changes.

Write 2–8 lines on why/intent/constraints — not a restatement of the diff. Be specific, no filler.

For multiple changes: one dominant change in the header, secondary changes as `* Also:` bullets in the body.

**Output**

Print only the raw commit message. No explanations, no code fences.