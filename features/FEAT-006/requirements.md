# FEAT-006 requirements

## Goal

Let users switch the signed-in Project Manager interface between Light and Dark without leaving the current view.

## Scope

- One Light / Dark toggle in the shared signed-in header.
- No stored preference, account setting, database change, cross-device sync, or system-theme detection.

## Acceptance criteria

- [ ] Opening the app afresh or doing a full reload starts in Light.
- [ ] The toggle can be used with the mouse or keyboard and switches immediately in both directions without reloading, changing the current page, or losing in-progress work.
- [ ] Navigating within the running app preserves the current theme; a fresh opening or full reload resets to Light.
- [ ] Existing text and controls remain readable and usable in both themes. Verify with a private, temporary preview using synthetic data before final approval.
