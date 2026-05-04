# BR002 Bridge Angle Candidate

## Purpose

BR002 suggests alternative bridge angles that may reduce the unsupported span for a BR001 finding.

## When It Runs

BR002 runs only after BR001 has already emitted a real bridge-risk finding.

It does not emit when:

- no BR001 finding exists
- the related unsupported span is `0`
- previous-layer support data is unavailable
- no candidate reaches the minimum improvement threshold

## Candidate Angle Generation

Default candidate angles are:

- `0`
- `45`
- `90`
- `135`

When `--angle-step-degrees` is set, candidates are generated from `0` up to `< 180` by that step size.

Angles within `10` normalized degrees of the current bridge angle are excluded.

## Improvement Threshold

BR002 emits only when the best candidate improves the estimated unsupported span by at least `20%` by default.

At most two candidates are included. Ties are broken by:

1. shorter estimated unsupported span
2. smaller angle

## Why It Is Not Guaranteed

The recommendation is heuristic. The tool rotates a virtual segment through the BR001 unsupported-span midpoint and resamples support; it does not regenerate slicer toolpaths or guarantee print success.

## Applying Candidate Angles

Use the reported angle as a slicer-side experiment:

- regenerate G-code with the candidate bridge angle
- inspect the new bridge path
- verify that the unsupported span is actually reduced

## False Positives

- virtual candidate geometry may look better than the slicer can actually realize
- local print context beyond previous-layer support is ignored

## False Negatives

- useful slicer-specific bridge strategies may not be representable as a simple angle change
- coarse angle stepping may miss a better intermediate angle

## Test Cases

- no BR001 finding
- best candidate below threshold
- two candidate angles with deterministic ordering
- exclusion of near-current angles
