# FrameStats Spec

## Scope
- `Engine/Diagnostics/FrameStats.cs`

## Contracts

### 1) Per-frame counters
- `BeginFrame()` MUST reset:
  - `DrawCalls`
  - `SpritesDrawn`
  - `SpritesCulled`
- `AddDrawCall`, `AddSpriteDrawn`, `AddSpriteCulled` MUST increment corresponding counters by one.

### 2) Timing update (`EndFrame`)
- If `deltaTime <= 0`, method MUST return without updating frame timing aggregates.
- `FrameMs` MUST be `deltaTime * 1000` when `deltaTime > 0`.
- Internal sampling window MUST accumulate time and frame count.
- When sampled time reaches at least `0.5` seconds:
  - `FrameMsAvg` MUST be recomputed from average sampled delta.
  - `FpsAvg` MUST be recomputed as reciprocal of average sampled delta.
  - Sample accumulators MUST reset.

### 3) Metadata
- `SetViewport`, `SetBackend`, `SetVSync`, `SetSceneName` MUST update corresponding properties immediately.
