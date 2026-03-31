# Camera Composer Spec

## Scope
- `Engine/CameraComposer.cs`
- `Engine/Camera.cs`
- `Engine/CameraInfluence.cs`
- `Engine/CameraInfluenceComponent.cs`

## Apply Contracts (`CameraComposer.Apply`)

### 1) No-op behavior
- If `influences` is empty, camera state MUST remain unchanged.

### 2) Influence priority and accumulation
- `SetCenter`:
  - Influence with highest priority MUST win.
  - For equal priority, later influence in traversal order MUST win (current `>=` behavior).
- `AddOffset`: all offsets MUST be summed.
- `Shake`: all shake offsets MUST be summed.
- Final offset MUST be `offset + shakeOffset`.

### 3) Target position
- `targetX = finalCenter.X - viewportWidth / 2 + finalOffset.X`
- `targetY = finalCenter.Y - viewportHeight / 2 + finalOffset.Y`

### 4) Clamp bounds
- `ClampBounds` with highest priority MUST be used.
- If bounds are smaller than viewport on axis:
  - Position MUST be pinned to axis minimum (`minX` / `minY`).
- Otherwise, position MUST be clamped to `[min, max]`.

### 5) Integer rect and subpixel offset
- `Rect.X` and `Rect.Y` MUST be `floor(targetX)` and `floor(targetY)`.
- `SubpixelOffset` MUST equal fractional remainders:
  - `(targetX - Rect.X, targetY - Rect.Y)`.

## Edge Cases
- Mixed influences in one frame (`SetCenter + AddOffset + Shake + ClampBounds`) MUST produce deterministic result.
- Degenerate bounds (`max < min`) MUST follow pin-to-min behavior.
