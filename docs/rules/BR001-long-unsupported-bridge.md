# BR001 Long Unsupported Bridge

## Purpose

BR001 detects bridge segments whose longest unsupported span is large enough to be worth warning about before printing.

## What It Detects

- direct bridge features classified as `BridgeInfill` or `InternalBridgeInfill`
- heuristic bridge candidates when slicer comments are missing or incomplete and support coverage is below `50%`

## Why It Matters

Long unsupported spans can sag, detach, or produce poor top surfaces even when a slicer preview looks acceptable at a glance.

## Default Thresholds

- warning: `25 mm`
- critical: `50 mm`

## Exact Threshold Comparison

Threshold comparison is inclusive: `>=`.

## Position Semantics

- `position.kind = unsupportedSpanMidpoint`
- the reported XY position is the midpoint of the longest unsupported interval run, not necessarily the segment start

## Support Sampling

- the candidate segment is split into deterministic intervals using `sampleSpacingMm`
- each interval is evaluated at its midpoint
- an interval is supported when the midpoint is within `supportToleranceMm` of any previous-layer extrusion segment
- support coverage is length-weighted by interval length

## Heuristic Bridge Detection

When `--detect-heuristic-bridges true`, BR001 may analyze `Unknown`, `SolidInfill`, `TopSolidInfill`, and `InternalInfill` if:

- the segment length is at least the warning threshold
- support coverage is below `50.0`

Perimeters are excluded from heuristic bridge detection in the MVP.

## How To Fix

- try changing the bridge angle for the region
- add local support or support enforcers
- add sacrificial internal walls or ribs
- reduce bridge speed and verify flow and fan settings
- split or reorient the model

## False Positives

- missing or incomplete slicer comments can force heuristic classification
- segment-distance support estimation is approximate

## False Negatives

- support that depends on more exact bead geometry can be missed
- only previous-layer support is considered

## Test Cases

- unsupported span exactly at warning threshold
- unsupported span exactly at critical threshold
- heuristic bridge detection with missing `;TYPE:` comments
- support coverage exactly `50.0`
