# Instructions for Codex: RaceIT.ProjectManager

## Before changing anything

- Start with [memory-bank/README.md](memory-bank/README.md). Read the current design, the active context, and the progress notes; consult the patterns and the technical context as the task requires.
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
- The app and the local SQL integration use SQL Server Express on Windows. Do not start the app against an existing database without checking the target: in Development it may seed data.
- Do not create or apply migrations, or delete, recreate, or change databases or data, without explicit authorization. The SQL steps in the technical context are for manual execution.
- When a material change is finished, update only the affected notes (usually `activeContext.md` and `progress.md`), preserve the history, and report what you verified. Do not store credentials or replace the user's data with sample data.

## Feature flow

For a feature, use the [feature flow](features/README.md). Jarvis creates the GitHub issue in `Draft` with only the requested scope. Edgar approves that exact scope, then Jarvis marks the issue `ready`. Once ready, Codex runs the complete flow: Requirements Analyst (tagged Gherkin) → Architect → Developer → Reviewer → QA. Do not require a second approval of Gherkin that stays within the approved scope. A material scope change requires Edgar's approval unless he expressly delegated that decision for this feature; record the decision and its authority.

The supplied issue is the canonical record. Codex authors, posts, and reads back its own complete feature record, requirements, design, handoffs, and QA plan/results as separate comments. If posting or verification is unavailable, stop the phase and report BLOCKED to Jarvis; Jarvis must not write or post Codex's artifacts. Do not create new `features/FEAT-NNN/` files or implementation logs. Preserve all historical feature records, including FEAT-007, and their decisions. Edgar retains required UI review, merge, and deployment approvals; technical approval does not replace them. Jarvis marks `completed` only after Edgar confirms the feature is finished.

When Jarvis launches the workflow, this Codex task coordinates all phases without a new authorization between phases within approved scope. Check the repository work tree, the supplied issue and comment access, scope approval, and the available `pm_*` agent types before starting. Spawn each role by its actual `agent_type`, not by naming a generic subagent after it. After Analyst posts and verifies Gherkin, Architect verifies the exact approved scope and requirements comment, changes `ready` to `inprogress`, and verifies the labels before design; separate approval of the requirements comment is not required. If a required capability or gate is missing, stop and report the blockage to Jarvis. See the Jarvis activation instructions in the feature flow.
