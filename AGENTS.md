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

For a feature, use the [feature flow](features/README.md). The GitHub issue supplied by the direct user or Jarvis is the canonical record for new work. Post the complete feature record, requirements, design, handoffs, and QA plan/results as separate comments on that issue; do not create new `features/FEAT-NNN/` files or implementation logs. Existing feature files remain historical evidence. Require tagged Gherkin acceptance scenarios in the requirements comment and owner approval of that exact comment revision before `Ready`. Invoke the Codex roles per phase; validate QA and UI approval where applicable. Technical approval does not authorize publishing or merging a PR.

When Jarvis launches the workflow, this Codex task coordinates the phases. Check the repository work tree, the supplied issue and comment access, and the available `pm_*` agent types before claiming work. Spawn each role by its actual `agent_type`, not by naming a generic subagent after it. The Architect, as the first agent after `Ready`, changes the authorized issue's label from `ready` to `inprogress` and verifies it before design work. If the runtime lacks a required role or issue access, report the missing capability to Jarvis and leave that phase unclaimed; Jarvis can start a Codex task in an environment that exposes the project roles and issue comments. See the Jarvis activation instructions in the feature flow.
