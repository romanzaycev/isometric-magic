# SpriteHolder Spec

## Scope
- `Engine/SpriteHolder.cs`
- `Engine/Sprite.cs`

## Contracts

### 1) Add behavior
- `Add(sprite, tag)` MUST maintain ascending order by `Sprite.Sorting` within that tag.
- For equal sorting values, insertion MUST be stable by append-after-equals behavior.
- Re-adding the same sprite to the same tag MUST be idempotent (no duplicates).

### 2) Remove behavior
- `Remove(sprite)` MUST remove sprite from all tags it belongs to.
- Internal indices MUST remain valid after removal.

### 3) Reindex behavior
- `TrySetReindex(sprite, oldSorting, newSorting)`:
  - MUST no-op when sprite is not indexed.
  - MUST no-op when `oldSorting == newSorting`.
  - MUST restore sorted order by local adjacent swaps when sorting changes.

### 4) Query behavior
- `GetSprites(tag)` for unknown tag MUST return empty read-only list.
- `GetSprites(tag)` for known tag MUST reflect current sorted order.
