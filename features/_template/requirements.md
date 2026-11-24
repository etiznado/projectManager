## Requirements — issue #NNN — revision 1

Content guide for Codex Analyst's **requirements issue comment**, produced after Edgar approves scope and Jarvis marks the issue ready. Replace #NNN with the supplied issue number; do not create a feature file. Link the exact approved scope and approval evidence. In-scope Gherkin needs no second approval. A material scope change requires Edgar's approval through Jarvis unless expressly delegated for this feature; record the authority and decision and stop affected work until resolved.

## Goal

State the user-visible outcome in one or two sentences. Record only what the owner confirmed.

## Scope

- Approved scope revision and Edgar's approval evidence:
- Consistency of these scenarios with approved scope:
- Included:
- Excluded:

## Acceptance scenarios

Write all observable acceptance criteria here, not as a separate prose checklist. Use one unique `@AC-NN` tag per scenario and concrete `Given`, `When`, and `Then` steps. Include at least one normal path and one relevant rejection or exception path. Add scenarios only when they express distinct behavior. If no rejection or exception path applies, explain that immediately below the code block.

```gherkin
Feature: <user-visible capability>

  @AC-01
  Scenario: <normal outcome>
    Given <specific starting state>
    When <actor takes a meaningful action>
    Then <observable result>

  @AC-02
  Scenario: <rejected or exceptional outcome>
    Given <specific starting state>
    When <actor attempts an invalid or exceptional action>
    Then <observable response and unchanged or corrected state>
```

## Open questions and assumptions

- List unresolved questions and unapproved assumptions here. Do not present them as accepted scenarios.

Codex posts and reads back this full comment, then reports its permalink, ID, and content revision for Architect. If posting or verification fails, stop and report BLOCKED to Jarvis; Jarvis must not draft or post this artifact. Follow [the workflow](../README.md) for versioned changes and continuation without another approval within scope.
