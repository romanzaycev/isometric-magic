# IonMotion Project Guidelines

## Architectural Scheme (Current)

The repository is split into two runtime layers with a hard assembly boundary:

- `Engine/IonMotion.Engine.csproj` - engine library with public API + internal implementation details.
- `ion-motion.csproj` - game executable that references only the engine library.

This split is not cosmetic. It is the primary encapsulation mechanism: engine internals are hidden from game code via `internal` and must stay hidden.

### Layering Rules

- Public engine API lives in non-`Core` namespaces and is intended for game-side usage and extension.
- Engine implementation details live under `IonMotion.Engine.Core.*` and are strictly non-exported (`internal`).
- Game code must depend on public API only and must not reference engine internals.
- Directory structure mirrors namespaces. Moving code between layers requires explicit architectural intent.

### Public vs Internal Contract

- Public layer: gameplay-facing abstractions (scene graph, rendering-facing API, assets-facing API, diagnostics/log facade, timing facade).
- Internal layer (`Core`): platform/bootstrap wiring, backend internals, resource holders, low-level runtime orchestration.
- If a feature needs to be configurable by game code, expose a minimal public facade instead of promoting internal classes.

### Extension Model

- The game can extend rendering behavior through the engine's supported public extension points (materials/effects).
- Backend replacement from game code is intentionally unsupported.
- Runtime timing is exposed through `Time.DeltaTime` (read-only for game code; engine-owned updates).

### Scene Graph Invariants

- Entities do not move between scenes at runtime.
- Cross-scene entity reparenting/migration is unsupported by design.

## Project Structure
- `Engine/` - engine library source and architecture layers (`Core` internal + public API namespaces)
- `Game/` - game-specific code using engine public API
- `resources/` - game assets (textures, configs)
- Root level: `Program.cs`, project files, `config.ini`

## Resources Layout

The project expects a `./resources/` directory at runtime (relative to the working directory).

- `resources/data/`:
  Hand-authored, source-controlled game content loaded by the game at runtime.
  Examples: `resources/data/maps/*.json`, `resources/data/sets/*.json`, `resources/data/textures/**`.

- `resources/data/textures/**`:
  Source textures used during development. Normal maps live next to their albedo textures as `*_normal.png`
  (they are not treated as generated assets, and not all textures will have them).

- `resources/_gen/`:
  Generated artifacts produced by the asset pipeline (e.g. packed texture atlases).
  This directory must be gitignored. If game data references an asset under `_gen/`, it must exist at runtime
  (no fallback behavior is allowed).

- `resources/pipeline/`:
  Source-controlled pipeline configuration files (atlas packing, normal map generation, etc).
  Pipeline jobs should read inputs from `resources/data/` and write outputs to `resources/_gen/`.

- `resources/engine/`:
  Engine-owned runtime assets (e.g. fonts) used by engine services.

### Resource Referencing Rules

- Game-authored JSON in `resources/data/**` may reference generated assets by paths starting with `_gen/...`
  (paths are resolved relative to `./resources`).
- Atlas outputs live under `resources/_gen/atlases/` (e.g. `_gen/atlases/<name>.json` plus textures).
- If a tileset declares `atlas`, missing atlas files or missing atlas regions are treated as errors.

### Pak Files and VFS

- Pak files are discovered automatically from `resources/paks/*.pak` at startup.
- Pak mount order is natural filename sort; later files override earlier files.
- Disk files under `resources/**` are mounted as a higher-priority layer and override pak entries.
- Pak input scope is `resources/data/**` and `resources/_gen/**`; `resources/engine/**` remains disk-only.
- Pak format header starts with ASCII magic `1MPAQ!`; header is plain, while index and file blobs use XOR obfuscation.

## Coordinate Semantics (Game)
- `IsoWorldPosition` and `CanvasPosition` are distinct value types in `Game/Model` and must not be mixed implicitly.
- `CanvasPosition` is the primary game-facing positioning API for rendering, camera, and VFX.
- `IsoWorldPosition` is a world/gameplay coordinate and is treated as a derived source for canvas placement.
- One-way position flow for entities is required: `IsoWorldPositionComponent` -> `IsoWorldToCanvasPositionSyncComponent` -> Entity.Transform.LocalPosition (`Transform2D`).
- `IsoWorldPositionComponent` stores only isometric world-space coordinates (world units, `float`).
- Conversion between spaces is centralized in `IsoWorldPositionConverter` via typed methods only:
  - `ToCanvas(IsoWorldPosition)`
  - `ToIsoWorld(CanvasPosition)` (allowed for input/picking helpers, not as an entity positioning mode)
  - `ToIsoTileCanvas(int tileX, int tileY)`
- On engine boundaries, use explicit bridging (`ToVector2()` / `FromVector2(...)`) to prevent semantic drift.

## Naming Conventions
- **Classes/Structs/Enums**: PascalCase (e.g., `SceneManager`, `CharacterState`)
- **Interfaces**: PascalCase with `I` prefix (e.g., `IGraphics`, `ICameraController`)
- **Methods/Properties**: PascalCase (e.g., `GetInstance()`, `Update()`)
- **Public fields**: PascalCase (e.g., `WorldPosX`, `Visible`)
- **Private fields**: `_camelCase` with underscore prefix (e.g., `_isInitialized`, `_deltaTime`)
- **Static readonly fields**: PascalCase (e.g., `Instance`, `SceneManager`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAIN`, `UI`)
- **Enums values**: PascalCase (e.g., `Idle`, `Running`)

## Import Organization
Order imports as follows:
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

using SDL2;
using Newtonsoft.Json;
using IniParser;

using IonMotion.Engine;
using IonMotion.Game.Scenes;
```

## Formatting
- **Braces**: K&R style (opening brace on same line)
- **Indentation**: 4 spaces (no tabs)
- **Max line length**: 120 characters
- **Use `var`** when type is obvious from right-hand side
- **Prefer expression-bodied members** for simple properties

## Error Handling
- Use try-catch in entry points (Program.cs, ApplicationBuilder.cs)
- Write errors to `Console.Error`
- Re-throw after logging unhandled exceptions
- Use pattern matching for switch expressions (C# 8.0+)

## Code Patterns
- **Singletons**: Use static readonly instance field + private constructor
- **Static accessors**: `GetInstance()` method for singletons
- **Virtual methods**: Use for lifecycle hooks (Initialize, Update, DeInitialize)
- **Coroutine pattern**: Use `IEnumerator` for async loading scenes
- **Protected fields**: Use for shared state accessible by subclasses

## Key Dependencies
- `SDL2` - Graphics and input (via P/Invoke bindings)
- `Silk.NET.OpenGL` - OpenGL bindings library
- `Newtonsoft.Json` (13.0.1) - JSON parsing
- `ini-parser-netstandard` (2.5.2) - INI config files

## Important Notes
- Project uses `AllowUnsafeBlocks` for SDL2 interop
- No solution file (.sln) - build directly from csproj
- SDL2 native libraries must be available in system PATH or project root
- Configuration via `config.ini` (window size, fullscreen, VSync, FPS, Logging, etc)
