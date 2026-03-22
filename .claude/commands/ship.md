Commit all local changes, push the branch, and open a GitHub PR.

## Steps

### 1. Check for changes
Run `git status` and `git diff HEAD` in parallel.
If the working tree is clean (no staged or unstaged changes, no untracked files), stop and tell the user — nothing to ship.

### 2. Check the current branch
Run `git branch --show-current`.

**If on `main` (or `master`):**
- Resolve the GitHub username: run `gh api user --jq '.login'`. If that fails (not authenticated), fall back to the local part of `git config user.email` (everything before `@`).
- Look at the diff to derive a short kebab-case slug describing the changes (e.g., `fix-dispose-override`, `add-jwt-validation`).
- If the changes are ambiguous, use AskUserQuestion to ask the user for a branch name.
- Create a new branch: `git checkout -b <username>/<slug>`

**If already on a feature branch:** continue in place.

### 3. Stage files
Inspect `git status` output and add modified/new files by name — prefer explicit paths over `git add -A`.
Skip any files that look sensitive (`.env`, `*.pfx`, `*secret*`, credentials).

### 4. Commit
- Review `git log --oneline -5` to match the existing commit style: `Verb Noun detail` (e.g., `Fix X509Source dispose override`, `Add JWT inner exception`).
- Pass the message via HEREDOC.
- Append the Co-Authored-By trailer.

### 5. Push
Run `git push -u origin HEAD`.
If the branch already tracks a remote and `git status` shows "up to date", skip the push.

### 6. Open PR
Run `gh pr create --base main` with a HEREDOC body:

```
## Summary
- <bullet 1>
- <bullet 2>

## Test plan
- [ ] Build passes (`make build`)
- [ ] Tests pass (`make test`)
- [ ] Manually verified <specific scenario if applicable>

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Return the PR URL to the user.

## Rules
- Never force-push (`--force`).
- Never amend an existing commit — always create a new one.
- Always target `main` as the PR base branch.
- If `gh` is not authenticated, tell the user to run `gh auth login` and stop.
