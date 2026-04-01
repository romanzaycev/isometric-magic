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
