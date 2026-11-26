# Instructions for Codex: RaceIT.ProjectManager

## Before changing anything

- Start with [docs/README.md](docs/README.md). Read the relevant product design, architecture, development and API documentation. Use the supplied issue for feature scope and acceptance criteria, and Codex/authorized PR artifacts for detailed handoffs and verification; do not maintain a separate feature-status diary in the repository.
- Cross-check the documentation against the code, the tests, and, where applicable, the migrations. Distinguish what is implemented, what is tested, and what is pending. Do not start pending work unless the user asks for it.
- Keep the repository conventions: namespace `RaceIT.ProjectManager`; .NET 10, Blazor Interactive Server, EF Core, and SQL Server.
- Preserve the original plan and do not edit the activities' original planned hours. Distinguish working hours, 24-hour waits, and manual dates; do not confuse plan with forecast. Edit activities in a modal, keep the selected context, and refresh the list when it closes.

## Verification and limits

From the root, with the .NET 10 SDK:

```text
dotnet build RaceIT.ProjectManager.sln
dotnet test RaceIT.ProjectManager.sln
```

- The solution's tests do not require SQL Server. In Codex Cloud, use build and tests without assuming the local Windows instance exists.
- The app and the local SQL integration use SQL Server Express on Windows. Check the target and startup/migration behavior before starting against an existing database. Preserve any instance currently used for testing unless its restart or modification is authorized.
- Do not create or apply migrations, or delete, recreate, or change databases or data, without explicit authorization. Follow [docs/development.md](docs/development.md) and [docs/local-test.md](docs/local-test.md); example commands do not authorize SQL operations.
- When a material change affects durable behavior or operation, update only the relevant documentation in `docs/` and report what you verified. Keep feature status, approvals and the final summary on the issue under the publication rules below; keep detailed execution evidence in Codex or an authorized PR. Do not create context/progress diaries, store credentials or replace the user's data with sample data. Preserve historical feature records and Git history.

## Feature flow

For a feature, use the [feature flow](features/README.md). Jarvis creates the GitHub issue in `Draft` with only the requested scope. Edgar approves that exact scope, then Jarvis marks the issue `ready`. Once ready, functional work follows Requirements Analyst (tagged Gherkin) → Architect → Developer → Reviewer → QA. Qualifying presentation-only adjustments to existing UI use the [visual-only fast path](features/README.md#visual-only-fast-path) by default, without asking Edgar to waive the cycle each time. Do not require a second approval of Gherkin that stays within the approved scope. A material scope change requires Edgar's approval unless he expressly delegated that decision for this feature; record the decision and its authority.

For visual-only work, the coordinator implements directly or continues with an already assigned Developer; do not restart Analyst, Architect, Reviewer or QA, rewrite Gherkin, or require omitted phase artifacts/capabilities. Verify the diff, compile the affected project/assets when needed, and inspect the result and affected interactions in the browser. Do not run the full solution test suite or add tests that mirror CSS. Changes to actions, event handling, validation, permissions, services, data or persistence follow the normal flow. Scope authorization, Edgar's required UI review, and SQL/data/runtime/publication limits still apply; reuse existing authorization without requesting it again.

The supplied issue is the canonical record for approved scope and acceptance criteria, approved scope changes, blockers requiring Edgar's intervention, and one final Codex summary (implemented, verified, pending, and available PR/evidence links). This publication rule applies to Jarvis-launched work and manually started Codex chats. Do not post startup comments, routine handoffs, repeated confirmations, full logs, or extensive phase artifacts. Keep detailed design, handoffs, phase verification, and technical evidence in Codex or, when useful and authorized, in the PR. Each invoked role authors its own artifact; the coordinator verifies every required handoff and exact governing revision without publishing a new feature record at each gate. The visual-only fast path needs only a brief record of its classification, exact diff, checks and limits in Codex and the usual final issue summary.

Before an allowed issue publication, reread to avoid duplicates and read back the posted body. If a required publication or its verification is unavailable, stop affected work and report BLOCKED to Jarvis or Edgar in Codex; Jarvis must not draft or post Codex's artifacts. A routine internal handoff needs no issue comment or publication check. Do not create new `features/FEAT-NNN/` files or implementation logs. Preserve historical records, including FEAT-007 and existing issue comments. Treat `features/_template/` as content guides only; their legacy full-artifact publication directions conflict with this rule. Document incompatible instructions elsewhere or in a monitor in Codex or the PR without changing files or monitor behavior outside approved scope. Edgar retains required UI review, merge, and deployment approvals; technical approval does not replace them. Jarvis marks `completed` only after Edgar confirms the feature is finished.

For the full flow, Codex coordinates all phases without a new authorization between phases within approved scope, including in manual chats. Check the repository work tree, the supplied issue and comment access, scope approval, and the `pm_*` agent types required by the chosen path before starting; do not post a comment merely to test access. Spawn each required role by its actual `agent_type`, not by naming a generic subagent after it. After Analyst records and verifies the acceptance Gherkin on the issue, Architect verifies the exact approved scope and requirements revision, changes `ready` to `inprogress`, and verifies the labels before design. No separate approval of in-scope Gherkin is required. Missing required capabilities or failed gates still stop affected work; publish a blocker only when Edgar's intervention is needed. See the feature flow for artifact references, verification, and publication limits.
