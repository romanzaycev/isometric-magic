# Isometric Magic Game Project

## Project Overview
A C# isometric 2D game engine built on SDL2 and OpenGL, targeting .NET 10.0.

## Build Commands

```bash
# Show available commands
make

# Restore all dependencies (NuGet + npm)
make restore

# Build the project (Debug by default)
make build

# Run the game
make run

# Run tests
make test-engine
make test-editor
make test

# Publish game
make publish
make publish-linux-x64
make publish-win-x64

# Clean .NET build artifacts
make clean

# Full required validation
make verify

# Optional direct .NET commands
dotnet build
dotnet test tests/IsometricMagic.Engine.Tests/IsometricMagic.Engine.Tests.csproj
dotnet test tests/IsometricMagic.RuntimeEditor.Tests/IsometricMagic.RuntimeEditor.Tests.csproj
dotnet publish
dotnet publish -r linux-x64
dotnet publish -r win-x64
dotnet clean
dotnet restore
```

## Runtime Editor SPA Notes

- Debug game build includes RuntimeEditor and requires SPA bundle in `Editor/IsometricMagic.RuntimeEditor/Web/dist/`.
- `make build`/`make run` (Debug) and `make test-editor` automatically run SPA build.
- SPA dependencies are installed only with `npm ci` via `make restore` or `make restore-spa`.

## Mandatory Verification

- Any code change MUST be validated with `make verify` before considering the task done.
- `make verify` enforces both `dotnet build` and all test suites (`engine` + `runtime editor`).
- This is a hard requirement and applies even if only non-engine files were changed.

## Architecture

Read `ARCHITECTURE.md` for details.

## Hard Architecture Guardrails

- `IsometricMagic.Engine.Core.*` is strictly internal implementation space.
- HARD RULE: never change visibility of any `Core` type/member to `public` (or otherwise export it) without explicit user instruction in the current task.
- HARD RULE: never introduce game-side dependencies on `Core` namespaces.
- HARD RULE: do not bypass the established structure (assembly boundary, namespace-to-directory alignment, public-vs-core separation) without explicit user permission.
- If a game-facing capability is needed, add or extend a public facade/API instead of exposing `Core` internals.
