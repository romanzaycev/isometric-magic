# Isometric Magic Game Project Guidelines 

## Project Structure
- `Engine/` - Core engine classes (graphics, input, scenes, camera)
- `Game/` - Game-specific code (characters, maps, animations, controllers)
- `resources/` - Game assets (textures, configs)
- Root level: `Program.cs`, project file, `config.ini`

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

using IsometricMagic.Engine;
using IsometricMagic.Game.Scenes;
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
