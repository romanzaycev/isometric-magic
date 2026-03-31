# Particles Curves and Gradients Spec

## Scope
- `Engine/Particles/FloatCurve.cs`
- `Engine/Particles/ColorGradient.cs`

## FloatCurve Contracts

### 1) Key setup (`SetKeys`)
- `null` or empty keys:
  - MUST reset to default constant curve with keys at `t=0` and `t=1` and value `1`.
- Input key times MUST be clamped to `[0..1]`.
- Keys MUST be sorted by time ascending.
- Single key input MUST be expanded to two keys with second key at `t=1` and same value.

### 2) Resolution
- `Resolution` MUST be clamped to minimum `2`.
- Changing resolution MUST invalidate LUT and trigger rebuild on next evaluate/prepare.

### 3) Evaluation
- `Evaluate(t)` MUST clamp `t` to `[0..1]`.
- Evaluation MUST read from LUT using deterministic index mapping.
- Interpolation between keys MUST be linear within each segment.

## ColorGradient Contracts

### 1) Key setup and defaults
- Same key handling rules as `FloatCurve`.
- Default value MUST be white (`Vector4(1,1,1,1)`).

### 2) Resolution and evaluation
- Same resolution and clamped evaluation rules as `FloatCurve`.
- Interpolation MUST be linear per channel (`Vector4` lerp).

## Determinism
- Repeated `Evaluate` with unchanged inputs MUST return stable values.
