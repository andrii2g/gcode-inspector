# Architecture

`gcode-inspector` is split into a thin CLI project and a reusable core analysis project.

## Projects

- `A2G.GCodeInspector.Cli`
  - Parses command arguments.
  - Handles exit-code mapping.
  - Routes analysis output to text, Markdown, or JSON writers.
  - Resolves `tool.version` from the CLI assembly informational version.
- `A2G.GCodeInspector.Core`
  - Parses modal G-code into layers and toolpath segments.
  - Classifies slicer `;TYPE:` comments.
  - Samples previous-layer support coverage.
  - Evaluates bridge-risk rules.
  - Produces deterministic in-memory analysis reports.
- `A2G.GCodeInspector.Tests`
  - Verifies parser, geometry, rules, analysis orchestration, CLI behavior, and JSON contract.

## Data Flow

1. CLI reads the requested G-code file.
2. `GCodeParser` converts modal commands into `PrintLayer` and `ToolpathSegment` records.
3. `ToolpathIndex` exposes flattened lookup structures for analysis.
4. `GCodeAnalyzer` runs rules in deterministic order.
5. Writers serialize the report according to the selected format.

## Core Concepts

- Parser state is modal. Omitted coordinates inherit the previous machine state.
- Layer Z is driven by actual extrusion Z, not slicer comment metadata.
- Support detection is interval-midpoint sampling against previous-layer extrusion segments.
- BR001 is the primary risk rule.
- BR002 is advisory and runs only after BR001 findings exist.

## Determinism

- Rule order is fixed.
- Findings are sorted deterministically.
- JSON uses explicit DTO ordering and alphabetically ordered metric keys.
- Numeric values are rounded only during report serialization.
