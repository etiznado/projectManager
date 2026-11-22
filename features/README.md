# Feature workflow with Jarvis and Codex

Jarvis coordinates new features and hands work to the project agents in this order: Requirements Analyst → Architect → Developer → Reviewer → QA. A feature request from the owner authorizes drafting requirements, not implementation, SQL changes, publication, or merge. Jarvis records decisions and the exact revision passed between phases; agents do not start themselves.

## Feature records

For each new feature, choose the next unused three-digit ID across both existing `features/FEAT-NNN.md` files and `features/FEAT-NNN/` directories. Create one `features/FEAT-NNN/` directory using the files in [`_template`](_template/):

- `README.md`: ID, title, status, UI review setting, decisions, handoffs, and verification summary.
- `requirements.md`: concise goal and scope, followed by the acceptance criteria as Gherkin scenarios in a `gherkin` code block. The Requirements Analyst drafts it from the owner's informal request.
- `design.md`: technical approach, affected files, risks, UI impact, and verification strategy. The Architect writes it against approved requirements and current code.
- `test-plan.md`: a small set of positive and negative validation cases, execution results, evidence, and limits. QA writes it after review.

Keep `FEAT-001.md` through `FEAT-005.md` as historical records. Do not move or reinterpret their approvals. A new feature's `README.md` is authoritative for its state and decision references; `requirements.md` is authoritative for approved scope and criteria. Code and test evidence determine what was actually implemented and verified.

## Handoffs and gates

1. **Requirements Analyst** (`pm_requirements_analyst`) creates `requirements.md` in `Draft` from the owner's request. Every observable acceptance criterion belongs in a Gherkin `Scenario` or `Scenario Outline` with `Given`, `When`, and `Then`, a unique `@AC-NN` tag, and concrete observable outcomes. Cover a normal path and a relevant rejection or exception path; if no exception applies, explain why. Keep prose for context and scope, not a second acceptance-criteria list. Separate confirmed requirements from proposals and ask Jarvis to resolve material ambiguities with the owner. Jarvis checks that the scenarios cover the requested behavior and records the owner's approval of the exact requirements revision before marking `Ready`. Later scope or scenario changes need renewed approval.
2. **Architect** (`pm_architect`) reads approved requirements, project context, code, and tests, then writes `design.md`. It reports decisions needing owner input through Jarvis. Design completion does not itself authorize implementation.
3. **Developer** (`pm_developer`) implements the authorized scope, adds appropriate implementation tests, and reports changed files, checks, limits, and an exact revision or artifact identifier. Jarvis marks `In Progress` only when execution is authorized and claimed.
4. **Reviewer** (`pm_reviewer`) independently reviews that revision against requirements and design, checks regressions and test evidence, and may make necessary code and test corrections within the approved scope. It records corrections and the resulting revision. Material scope changes go to the owner through Jarvis. QA checks the final revision, including reviewer corrections.
5. **QA** (`pm_qa`) writes `test-plan.md` with at most six focused cases by default, covering relevant positive and negative behavior. Each case cites its `@AC-NN` scenario; every approved scenario has a case or an explicit coverage gap marked BLOCKED. QA runs permitted checks against the final reviewed revision and records PASS, FAIL, or BLOCKED with evidence per case. If QA finds a defect, Jarvis returns it to Developer, then Reviewer reviews the correction, then QA repeats on the new revision. Jarvis arranges additional UI or integration checks where required; unrun cases cannot be marked PASS.

Jarvis verifies every handoff, keeps the feature record current, and stops at a failed gate. No role changes feature states, publishes, deploys, pushes commits, opens or merges PRs, or runs SQL or migrations merely because its phase is complete. The owner retains scope, UI, publication, and merge approvals.

## States

| State | Gate |
| --- | --- |
| `Draft` | Requirements are being drafted; no implementation authorization. |
| `Ready` | The owner approved the exact requirements revision; its tagged Gherkin scenarios cover the acceptance criteria and UI review needs are stated. |
| `In Progress` | An authorized execution is claimed; Architect, Developer, Reviewer, and QA handoffs occur in order. |
| `UI Review` | QA passed and `ui_review: required`; the owner can interact with a private, temporary preview of the exact revision. |
| `In Review` | QA passed, required UI review was approved by the owner, and the final revision is ready for the owner's PR decision. |
| `Done` | The owner approved the final PR and merge of that exact revision was verified. |

When `ui_review: none`, explain why and move from QA PASS to `In Review`. A visible UI change invalidates earlier UI approval. A change to the final revision requires QA and any required UI review to repeat. Technical review never replaces the owner's approval.

For UI review, use a clickable, private, temporary preview with synthetic data and an exact revision reference. Do not use an existing SQL database without checking its destination and obtaining the required authorization. If a safe preview is unavailable, record the blockage. Do not publish a preview or PR without authorization.

## Jarvis activation

This repository defines the handoffs and artifacts for Jarvis to follow. It does not install a Dot trigger or grant Jarvis permissions. When configuring Jarvis, identify work by repository, feature ID, and approved requirements revision, claim it once before execution, and check for an existing run before retrying an uncertain launch. A repository file or `Ready` label alone is not permission to start implementation. The earlier read-only Dot scanning pilot remains historical; it is not an active trigger for this workflow.
