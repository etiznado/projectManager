## Test plan and results — issue #NNN — revision 1

Content guide for Codex QA's **issue comment**, not a feature file. Link Edgar's exact approved scope and approval evidence, governing requirements comment/revision, and final reviewed code revision. Do not require separate approval of in-scope Gherkin.

## Revision under test

- Final reviewed revision or artifact:
- Environment and test data:

## Validation cases

Keep at most six focused cases by default. Map each case to one or more governing `@AC-NN` scenarios within approved scope; identify any uncovered scenario as BLOCKED. Cover relevant positive and negative paths. Add cases only when the feature's risk requires them and record why.

| ID | Scenario tag | Type | Setup and action | Expected result | Actual result and evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `@AC-01` | Positive |  |  |  | Not run |
| 2 | `@AC-02` | Negative |  |  |  | Not run |

## Results and limits

- Build and automated tests:
- UI or integration checks:
- Overall result: PASS / FAIL / BLOCKED
- Defects, reproduction steps, and remaining limits:

Codex posts and reads back its own full QA comment and reports permalink, ID, and revision. If posting or verification fails, stop and report BLOCKED to Jarvis; Jarvis must not draft or post this artifact. Material scope changes require Edgar's approval unless expressly delegated for this feature; record the authority and decision and stop affected work until resolved. QA PASS does not replace required UI, merge, or deployment approvals or Edgar's completion confirmation. Jarvis alone marks completed after that confirmation. Follow [the workflow](../README.md).
