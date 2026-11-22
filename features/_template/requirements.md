## Requirements — issue #NNN — revision 1

Content guide for a **requirements issue comment**. Replace #NNN with the supplied issue number; do not create a feature file. Include the comment revision and links to the owner's request.

## Goal

State the user-visible outcome in one or two sentences. Record only what the owner confirmed.

## Scope

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
