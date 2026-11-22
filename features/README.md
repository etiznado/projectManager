# Feature workflow with Jarvis and Codex

Jarvis coordinates new features in this order: Requirements Analyst → Architect → Developer → Reviewer → QA. A feature request authorizes drafting requirements only. Implementation, SQL changes, publication, and merge retain their separate gates.

## Issue as the feature record

For new work, the supplied GitHub issue in this repository is the canonical feature record. A direct Codex request must include its issue number or URL; Jarvis supplies it when launching Codex. Confirm the issue belongs to the intended repository and read its body and existing comments before a phase begins. If the issue is missing, inaccessible, locked, or ambiguous, ask the requester or Jarvis for a usable issue and leave the phase unclaimed. Do not create an issue, choose another issue, or create `features/FEAT-NNN/` as a fallback.

Post the complete contents that would formerly have gone in `README.md`, `requirements.md`, `design.md`, and `test-plan.md` as **separate Markdown comments on that issue**. Use the corresponding files in [`_template`](_template/) as content guides only; never copy them into a new feature directory. The coordinator owns the feature record comment (state, decisions, approvals, handoffs, and verification summary). Analyst owns requirements, Architect design, and QA test plan. Developer and Reviewer post their handoff and findings as separate comments. Existing `FEAT-001`–`FEAT-006` files are historical records; do not move, delete, or reinterpret their decisions. If an existing feature is continued, read its files as prior evidence and put new phase output on the supplied issue, linking the old record.

Start every workflow comment with a clear heading such as `## Requirements — issue #123 — revision 1`, `## Design`, `## Developer handoff`, `## Reviewer handoff`, `## Test plan and results`, or `## Feature record`. Include the issue URL or number, role, status/result, and links to the exact input comments and code revision used. A comment's permalink and numeric comment ID identify its revision. Preserve complete Markdown, including the `gherkin` block and QA table, in the comment body. Do not use a summary comment in place of the required artifact.

Read the issue again immediately before posting to avoid duplicate output from a retry. After posting, read back the comment and give its permalink and ID to the coordinator or Jarvis. If a post or read-back fails, report BLOCKED; do not claim that the phase is complete. At each gate, the coordinator appends a new feature record comment linking the current valid revision of each artifact and the preceding feature record, so readers do not have to guess which revision governs.

For a correction before approval, the comment author may edit their own draft comment only after re-reading it and preserving the full replacement body. Identify the edited comment ID and its new revision in the handoff. For approved requirements, reviewed code, QA results, or any change that affects a gate, **append a new versioned comment** and link the superseded comment; do not silently overwrite the approved evidence. An amended requirements scenario or scope needs the owner's approval of the new comment before `Ready`. A code change after review needs Reviewer and QA again, plus UI review when required. Keep earlier comments for audit history. State and approval decisions belong in a new coordinator feature record comment with the owner's decision and exact target comment/revision; a role must not infer approval from a label or an old comment.

## Posting and updating comments

Use an available GitHub issue-comment connector for reading, creating, and (only when appropriate) editing comments. If it cannot create a normal issue comment, use an authenticated GitHub client or the GitHub REST issue-comment endpoints. The REST operations are `GET /repos/{owner}/{repo}/issues/{issue_number}/comments` to find existing comments, `POST /repos/{owner}/{repo}/issues/{issue_number}/comments` with a Markdown `body` to create one, and `PATCH /repos/{owner}/{repo}/issues/comments/{comment_id}` with the complete replacement `body` to edit an author-owned draft. The comment ID is **not** the issue number. Check the returned `html_url` and body. The GitHub CLI equivalent for a new comment is `gh issue comment ISSUE --repo OWNER/REPO --body-file PATH`; its `--edit-last` option is safe only if the last comment by that account is the intended draft. Prefer the comment-ID REST operation for an exact edit. If authentication or comment permissions are unavailable, return the full prepared Markdown to the coordinator and mark posting BLOCKED; the coordinator may post through an authorized connection and verify the permalink. Do not put tokens in files or comments.

## Handoffs and gates

1. **Requirements Analyst** (`pm_requirements_analyst`) posts a Draft requirements comment from the owner's request. Every observable acceptance criterion belongs in a Gherkin `Scenario` or `Scenario Outline` with `Given`, `When`, and `Then`, a unique `@AC-NN` tag, and concrete observable outcomes. Cover a normal path and a relevant rejection or exception path; explain if no exception applies. Keep prose for context and scope, not a second criteria list. Jarvis records the owner's approval of the exact comment revision before `Ready`.
2. **Architect** (`pm_architect`) reads the approved requirements comment, project context, code, and tests, then posts a design comment with affected files, data and UI impact, risks, and verification strategy. Design completion does not authorize implementation.
3. **Developer** (`pm_developer`) implements authorized scope, adds appropriate tests, and posts a handoff comment with changed files, checks, limits, and exact code revision or artifact identifier. Jarvis marks `In Progress` only when execution is authorized and claimed.
4. **Reviewer** (`pm_reviewer`) independently reviews that revision against requirements and design, may correct code and tests within scope, and posts findings, corrections, verification, and the final revision for QA. Material scope changes return to the owner through Jarvis.
5. **QA** (`pm_qa`) posts a test-plan and results comment with at most six focused cases by default. Each case cites its approved `@AC-NN` scenario; each scenario has a case or an explicit BLOCKED coverage gap. QA runs permitted checks against the final reviewed revision and records PASS, FAIL, or BLOCKED with evidence per case. A defect returns to Developer, then Reviewer, then QA on the new revision. Unrun cases cannot be PASS.

Jarvis verifies each posted comment and exact handoff, keeps the feature record current, and stops at a failed gate. No role changes feature states, publishes, deploys, pushes commits, opens or merges PRs, or runs SQL or migrations merely because its phase is complete. The owner retains scope, UI, publication, and merge approvals.

## States

| State | Gate |
| --- | --- |
| `Draft` | Requirements are being drafted; no implementation authorization. |
| `Ready` | The owner approved the exact requirements comment revision; tagged Gherkin scenarios cover the acceptance criteria and UI review needs are stated. |
| `In Progress` | An authorized execution is claimed; Architect, Developer, Reviewer, and QA handoffs occur in order. |
| `UI Review` | QA passed and `ui_review: required`; the owner can interact with a private, temporary preview of the exact revision. |
| `In Review` | QA passed, required UI review was approved by the owner, and the final revision is ready for the owner's PR decision. |
| `Done` | The owner approved the final PR and merge of that exact revision was verified. |

When `ui_review: none`, explain why and move from QA PASS to `In Review`. A visible UI change invalidates earlier UI approval. A change to the final revision requires QA and any required UI review to repeat. Technical review never replaces the owner's approval. For UI review, use a clickable, private, temporary preview with synthetic data and an exact revision reference. Do not use an existing SQL database without checking its destination and obtaining the required authorization. If a safe preview is unavailable, record the blockage. Do not publish a preview or PR without authorization.

## Jarvis activation

Jarvis starts a **Codex task** for this repository on the owner's connected computer, or in a configured Codex cloud environment. Jarvis coordinates the task and owner decisions; the Codex task invokes the project-scoped `.codex/agents/pm_*.toml` roles. Jarvis's background agents are not these Codex roles. Pass the actual Codex `agent_type`; a generic agent with a matching task name does not load its instructions. This repository does not install a Dot trigger or grant Jarvis permissions.

Before the first handoff, the coordinator runs `git rev-parse --show-toplevel`, checks `git status --short --branch`, and confirms all five `pm_*` role types are exposed. It also checks access to the supplied issue and its comment history. If the work tree, required role, or issue is unavailable, leave the phase unclaimed and report the missing capability to Jarvis. Do not retry by renaming generic subagents or by writing local feature files.

For each authorized phase, Jarvis sends the Codex task the repository, issue URL/number, exact approved requirements comment permalink (when applicable), phase, authorization, and scope. The coordinator invokes only that phase's real role, waits for its result, verifies the posted comment and code revision, and reports links and evidence to Jarvis. A request to draft requirements starts only Analyst. Before retrying an uncertain launch, Jarvis checks for an existing task and comment for that repository, issue, phase, and revision. An issue label or `Ready` text alone is not implementation authorization. The earlier read-only Dot scanning pilot remains historical; it is not an active trigger.
