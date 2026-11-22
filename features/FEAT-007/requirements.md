# FEAT-007 requirements

## Goal

Add a footer to the shared Project Manager page layout that displays only the current calendar year.

## Scope

- Included: The shared layout used by the sign-in page and signed-in pages.
- Excluded: Pages outside the shared layout and unrelated layout or behavior changes.

## Acceptance scenarios

```gherkin
Feature: Current-year footer

  @AC-01
  Scenario Outline: Show only the current year below the page content
    Given the current calendar year is 2026
    When a person opens <page> using the shared layout
    Then exactly one footer appears below the page content
    And its only visible content is "2026"

    Examples:
      | page             |
      | the sign-in page |
      | a signed-in page |

  @AC-02
  Scenario: Do not show the previous year after rollover
    Given the current calendar year has advanced from 2026 to 2027
    When a person opens a page using the shared layout
    Then the footer's only visible content is "2027"
    And no code change or redeployment is needed
```
