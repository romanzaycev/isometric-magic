# Engine ECS Lifecycle Spec

## Scope
- `Engine/Entity.cs`
- `Engine/Component.cs`

## Contracts

### 1) Component attachment
- Adding a component that already belongs to another entity MUST throw `InvalidOperationException`.
- If an entity already belongs to a scene (`Entity.Scene != null`) and a component is added:
  - `Awake` MUST be called immediately.
  - `OnEnable` MUST be called immediately only when `Entity.ActiveInHierarchy == true` and `component.Enabled == true`.

### 2) Component enabled state
- Toggling `Enabled` on an active-in-hierarchy component MUST call `OnEnable`/`OnDisable` exactly on state transitions.
- Toggling `Enabled` while the entity is inactive in hierarchy MUST NOT call `OnEnable`/`OnDisable`.

### 3) Entity active state propagation
- `ActiveInHierarchy` MUST follow:
  - `ActiveSelf` AND (`Parent.ActiveInHierarchy` OR (`Parent == null` AND `Scene != null`)).
- Changing `ActiveSelf` MUST recalculate hierarchy activity and propagate to descendants.
- For enabled components:
  - transition to active => `OnEnable`
  - transition to inactive => `OnDisable`
- Descendants with `ActiveSelf == true` MUST receive propagated hierarchy activity.

### 4) Update loop lifecycle
- `Start` MUST run exactly once per component, before the first `Update`, and only when entity is active in hierarchy.
- `Update` and `LateUpdate` MUST run only for `IsActiveAndEnabled` components.

### 5) Destroy lifecycle
- `Entity.Destroy()` MUST enqueue destruction and MUST NOT immediately invoke `OnDestroy`.
- During destroy queue processing:
  - `OnDestroy` MUST be called for each component.
  - Component storage MUST be cleared.
  - Children MUST be destroyed recursively.

## Edge Cases
- `SetParent` MUST correctly update scene assignment recursively.
- `SetParent` MUST correctly recompute hierarchy activity and fire enable/disable callbacks when state changes.
