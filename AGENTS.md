# Isometric Magic Game Project

## Project Overview
A C# isometric 2D game engine built on SDL2 and OpenGL, targeting .NET 10.0.

## Build Commands

```bash
# Build the project
dotnet build

# Run unit tests
dotnet test tests/IsometricMagic.Engine.Tests/IsometricMagic.Engine.Tests.csproj

# Build in Release mode
dotnet build -c Release

# Run the game
dotnet run

# Clean build artifacts
dotnet clean

# Restore dependencies
dotnet restore
```

## Mandatory Verification

- Any code change MUST be validated with both `dotnet build` and run tests before considering the task done.
- This is a hard requirement and applies even if only non-engine files were changed.

## Architecture

Read `ARCHITECTURE.md` for details.

## Hard Architecture Guardrails

- `IsometricMagic.Engine.Core.*` is strictly internal implementation space.
- HARD RULE: never change visibility of any `Core` type/member to `public` (or otherwise export it) without explicit user instruction in the current task.
- HARD RULE: never introduce game-side dependencies on `Core` namespaces.
- HARD RULE: do not bypass the established structure (assembly boundary, namespace-to-directory alignment, public-vs-core separation) without explicit user permission.
- If a game-facing capability is needed, add or extend a public facade/API instead of exposing `Core` internals.
