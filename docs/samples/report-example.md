# Report Example

## Text

```text
GCode Inspector analysis
File: bridge-warning-unsupported.gcode
Layers: 2
Segments: 3
Findings: 0 critical, 1 warning, 0 info

[WARNING] BR001 Long unsupported bridge
Layer: 1
Z: 0.4 mm
Line: 10
Position: X15.00 Y0.00 (unsupportedSpanMidpoint)
Metric: unsupportedSpanMm = 12.00
Suggestion: Try changing the bridge angle for this region.
```

This example assumes `--max-bridge-span-warning 10`.

## Markdown

```markdown
# GCode Inspector Report

## Summary

| Metric | Value |
| --- | ---: |
| File | `bridge-warning-unsupported.gcode` |
| Layers | 2 |
| Segments | 3 |
| Critical | 0 |
| Warning | 1 |
| Info | 0 |
```

## JSON

See the README sample for the full schema-shaped example.
