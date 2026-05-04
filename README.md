# gcode-inspector

`gcode-inspector` is a deterministic .NET CLI for static analysis of generated FFF/FDM G-code.

Slicer previews can show bridge regions visually, but they usually do not explain where a risky span is, how severe it is, or what change is worth trying next. This MVP focuses on bridge-risk analysis and candidate bridge-angle suggestions.

## What It Does

- Detects long unsupported bridge spans in generated G-code.
- Reports location, severity, and rule-specific evidence.
- Suggests slicer or model changes to try next.
- Emits text, Markdown, or JSON reports.

## Current Rules

- `BR001` Long unsupported bridge
- `BR002` Bridge angle candidate

## MVP Limitations

- Support detection is heuristic and segment-distance based, not exact bead-area reconstruction.
- Bridge angle recommendations are heuristic and must be verified by regenerating and inspecting G-code.
- Only previous-layer support is considered.
- Cura-specific feature-comment classification is not part of the MVP.
- `G2` and `G3` arcs are not interpreted in the MVP.

## Install From Source

```powershell
dotnet restore .\src\A2G.GCodeInspector.Tests\A2G.GCodeInspector.Tests.csproj --configfile .\NuGet.Config
dotnet build .\gcode-inspector.slnx --no-restore
```

## Run Analysis

```powershell
dotnet run --project .\src\A2G.GCodeInspector.Cli -- analyze .\src\A2G.GCodeInspector.Tests\TestData\bridge-critical-unsupported.gcode
dotnet run --project .\src\A2G.GCodeInspector.Cli -- analyze .\src\A2G.GCodeInspector.Tests\TestData\bridge-critical-unsupported.gcode --format json
dotnet run --project .\src\A2G.GCodeInspector.Cli -- analyze .\src\A2G.GCodeInspector.Tests\TestData\bridge-warning-unsupported.gcode --max-bridge-span-warning 10 --format markdown --output report.md
dotnet run --project .\src\A2G.GCodeInspector.Cli -- explain BR001
dotnet run --project .\src\A2G.GCodeInspector.Cli -- explain BR002
```

## Example Warning

The warning example below assumes `--max-bridge-span-warning 10`.

```text
[WARNING] BR001 Long unsupported bridge
Layer: 1
Z: 0.4 mm
Line: 10
Position: X15.00 Y0.00 (unsupportedSpanMidpoint)
Metric: unsupportedSpanMm = 12.00
Suggestion: Try changing the bridge angle for this region.
```

## Exit Codes

- `0`: analysis completed and the selected fail-on policy was not triggered
- `1`: analysis completed and `--fail-on warning` was triggered by warning or critical findings
- `2`: analysis completed and `--fail-on critical` was triggered by critical findings
- `64`: invalid CLI arguments
- `65`: input file cannot be read
- `66`: parser failed
- `67`: output file cannot be written
- `70`: unexpected runtime error

## Sample JSON Output

```json
{
  "file": {
    "name": "bridge-warning-unsupported.gcode",
    "path": "bridge-warning-unsupported.gcode",
    "sizeBytes": 180
  },
  "findings": [
    {
      "evidence": [
        "featureType=BridgeInfill",
        "sampleSpacingMm=12",
        "supportToleranceMm=0.45"
      ],
      "featureType": "bridgeInfill",
      "id": "BR001-0001",
      "layerIndex": 1,
      "lineNumberEnd": 10,
      "lineNumberStart": 10,
      "message": "Layer 1 at Z=0.4 mm contains a bridge segment with approximately 12 mm unsupported span.",
      "metrics": {
        "segmentLengthMm": 30,
        "supportCoveragePercent": 60,
        "supportedLengthMm": 18,
        "thresholdCriticalMm": 50,
        "thresholdWarningMm": 10,
        "unsupportedCoveragePercent": 40,
        "unsupportedSpanMm": 12
      },
      "position": {
        "kind": "unsupportedSpanMidpoint",
        "xMm": 15,
        "yMm": 0
      },
      "relatedFindingId": null,
      "ruleId": "BR001",
      "segment": {
        "angleDegrees": 0,
        "end": {
          "xMm": 30,
          "yMm": 0
        },
        "lengthMm": 30,
        "start": {
          "xMm": 0,
          "yMm": 0
        }
      },
      "severity": "warning",
      "suggestions": [
        "Try changing the bridge angle for this region."
      ],
      "title": "Long unsupported bridge",
      "zMm": 0.4
    }
  ],
  "options": {
    "angleMinImprovementPercent": 20,
    "angleStepDegrees": 45,
    "detectHeuristicBridges": true,
    "failOn": "none",
    "format": "json",
    "maxBridgeSpanCriticalMm": 50,
    "maxBridgeSpanWarningMm": 10,
    "sampleSpacingMm": 12,
    "supportToleranceMm": 0.45
  },
  "schemaVersion": 1,
  "summary": {
    "findings": {
      "critical": 0,
      "info": 0,
      "total": 1,
      "warning": 1
    },
    "layers": 2,
    "segments": 3
  },
  "tool": {
    "name": "gcode-inspector",
    "version": "0.1.0"
  }
}
```
