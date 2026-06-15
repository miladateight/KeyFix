# Security

KeyFix uses Windows keyboard hooks, so changes to input handling should be reviewed carefully.

## Reporting Issues

If you find a security or privacy issue, open a private security advisory on GitHub if the repository supports it. Otherwise, contact the maintainer directly before publishing details.

## Sensitive Areas

- `KeyboardHookService`
- `KeyboardLayoutService`
- Settings persistence
- Any future networking or telemetry code

## Project Policy

The default project should remain local-only and should not add telemetry, remote logging, or typed-text persistence.
