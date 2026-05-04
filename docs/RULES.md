# Rules

`gcode-inspector` currently exposes two bridge-related rules.

## BR001

- Title: `Long unsupported bridge`
- Severity: `warning` or `critical`
- Purpose: detect bridge segments whose longest unsupported span crosses configured thresholds

## BR002

- Title: `Bridge angle candidate`
- Severity: `info`
- Purpose: suggest alternative bridge angles that may reduce the unsupported span for a BR001 finding

## Notes

- `BR` is the bridge-rule family prefix.
- Rule IDs are stable and intended for CLI output, JSON output, tests, and documentation.
- BR002 is advisory only and never emits warning or critical severity in the MVP.
