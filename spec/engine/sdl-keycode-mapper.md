# SDL Keycode Mapper Spec

## Scope
- `Engine/SdlKeycodeMapper.cs`

## Contracts

### 1) Known keycodes
- Known `SDL_Keycode` values MUST map to expected internal `Key` values.
- `TryMap` MUST return `true` for known keycodes.

### 2) Unknown keycodes
- Unknown keycodes MUST return `false`.
- Unknown keycodes MUST NOT throw exceptions.

### 3) Unknown logging behavior
- Unknown key logging SHOULD be de-duplicated per keycode value.
- The same unknown keycode SHOULD be logged at most once during process lifetime.
