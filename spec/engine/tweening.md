# Tweening Spec

## Scope
- `Engine/Tweening/TweenManager.cs`
- `Engine/Tweening/TweenHandle.cs`
- `Engine/Tweening/Interp.cs`
- `Engine/Tweening/Easing.cs`

## TweenManager Contracts

### 1) Delay tween
- `Delay(seconds <= 0)`:
  - MUST invoke `onComplete` immediately (if provided).
  - MUST return invalid handle.
- `Delay(seconds > 0)`:
  - MUST complete only after accumulated `dt >= seconds`.
  - MUST invoke `onComplete` exactly once.

### 2) Value tween (`To`)
- If `duration <= 0` and `delay <= 0`:
  - Target value MUST be set immediately.
  - `onComplete` MUST be called immediately (if provided).
  - Returned handle MUST be invalid.
- If `delay > 0`:
  - Setter MUST NOT be called before delay elapses.
- During active tween:
  - `t = clamp01((elapsed - delay) / duration)` when `duration > 0`.
  - Eased time MUST be `ease(t)`.
  - Setter MUST receive `lerp(from, to, easedT)`.
- At completion (`t >= 1`):
  - `onComplete` MUST be called exactly once.
  - Tween MUST be removed from manager.

### 3) Cancel semantics
- `handle.Cancel()` MUST stop further updates and remove tween.
- After cancel, `handle.IsValid` MUST become `false`.

## Interp/Easing Contracts

### 1) Clamp
- `Interp.Clamp01(t)` MUST clamp to `[0..1]`.

### 2) Endpoint guarantees
- For each easing function (`Linear`, `InQuad`, `OutQuad`, `InOutQuad`, `InCubic`, `OutCubic`, `InOutCubic`):
  - `f(0) == 0`
  - `f(1) == 1`
