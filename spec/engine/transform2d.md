# Transform2D and MathHelper Spec

## Scope
- `Engine/Transform2D.cs`
- `Engine/MathHelper.cs`

## Transform2D Contracts

### 1) World position
- If `Parent == null`, `WorldPosition` MUST equal `LocalPosition`.
- If `Parent.WorldRotation == 0`, `WorldPosition` MUST equal `Parent.WorldPosition + LocalPosition`.
- If `Parent.WorldRotation != 0`, `LocalPosition` MUST be rotated by parent world rotation (`1.0 == 360deg`) and then translated by `Parent.WorldPosition`.

### 2) World rotation
- If `Parent == null`, `WorldRotation` MUST equal `LocalRotation`.
- Otherwise, `WorldRotation` MUST equal `Parent.WorldRotation + LocalRotation`.

### 3) Reparenting (`SetParent`)
- If `worldPositionStays == true`:
  - `WorldPosition` and `WorldRotation` MUST remain unchanged after reparenting.
  - `LocalRotation` MUST be normalized via `MathHelper.NormalizeNor`.
- If `worldPositionStays == false`:
  - Local transform values (`LocalPosition`, `LocalRotation`) MUST be preserved as-is when parent changes.

## MathHelper.NormalizeNor Contract

### 1) Normalization range and sign
- Normalization behavior MUST be explicitly defined and kept stable by tests.
- Required test inputs:
  - `0`, `0.25`, `1.0`, `1.2`, `1.6`, `-0.25`, `-1.2`, `-1.6`.

### 2) Compatibility note
- If behavior changes, update this spec and migration notes to avoid silent gameplay regressions (rotation snaps/drift).
