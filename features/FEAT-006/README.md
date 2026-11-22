---
id: "FEAT-006"
title: "Light / Dark mode toggle"
status: "In Progress"
ui_review: "required"
---

# Light / Dark mode toggle

## Decisions and handoffs

- Owner's scope approval: November 22, 2026, voice session; approved the toggle with reset to Light on fresh opening or full reload.
- Approved requirements: [requirements.md](requirements.md) in the initial Ready commit.
- Activation: manual execution authorized by Edgar on November 22, 2026 for the isolated checkout `feature/feat-006-theme-toggle` at `b2268bbfc875204004797760b35d00fa6b6b92dd`; requirements blob `cb86665c65e6363487bc816ce9189bd4704af7d2`. Claimed once for this local run.
- Architect (`pm_architect`): design completed; blob `4641588f087a4ee233bba29a9efd7623f19222c2`.
- Developer (`pm_developer`): local implementation on the isolated branch; final implementation blobs are listed in [test-plan.md](test-plan.md). Reviewer (`pm_reviewer`) independently approved the exact code and tests without corrections.
- QA (`pm_qa`): six focused cases recorded in [test-plan.md](test-plan.md), blob `c1a6d44af76dc35e87cf06334ba50acd2c7c3174`; one automated PASS and five browser cases BLOCKED.
- Owner UI review and final approval: pending. Status remains `In Progress`; no `UI Review` gate has been passed. No Git remote action is authorized.

## Verification summary

- Local working-tree candidate on `feature/feat-006-theme-toggle` from HEAD `b2268bbfc875204004797760b35d00fa6b6b92dd`; no commit. `dotnet build RaceIT.ProjectManager.sln`: 0 errors, 0 warnings. `dotnet test RaceIT.ProjectManager.sln`: 70/70 passed. `git diff --check`: passed. No app or SQL was started.
- QA is BLOCKED for feature acceptance and visual review: the signed-in preview requires an exclusively verified loopback with a synthetic SQL fixture. Development startup seeds SQL and provisional sign-in queries it. See the case-by-case evidence and exact code blobs in [test-plan.md](test-plan.md).
