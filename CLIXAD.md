# CLIXAD — Working in AFO (Against The Current)

## What it is
A Unity 6 URP project — a flood-disaster exploration game. The main menu ("Putter") leads to a gameplay scene ("Nate") and a map-building scene ("Kao").

## Layout
```
Assets/
  Putter/          — Main menu scene + UI code (MainMenuUI, fonts, editor builder)
  Nate/            — Gameplay scene (Nate.unity)
  Kao/             — Flood-disaster map assets, scene, PlayerController, builders
  Scripts/AFO/
    UI/            — Runtime UI components (MenuButton, motions, shapes, transitions)
    MapBuilder/    — Runtime flood map building scripts
Plugins/           — Third-party Unity packages
Scenes/            — SampleScene (default)
design/            — Screenshots, scene dumps, play-state inspector output
run_build.json     — CI entry point
```

## Build / run
- Open the project in Unity 6 with the **Universal Render Pipeline**.
- Scenes in build: `Putter.unity` (index 0), `Nate.unity` (1), `SampleScene.unity` (2).
- **Play** from `Putter.unity` — PLAY button loads `Nate.unity`.
- **Editor tools** (both rebuild their scenes from code):
  - `Tools/AFO/Rebuild Main Menu UI` — regenerates the Putter canvas.
  - `Tools/Kao Map/Build Map` — generates terrain, water, village, etc.
- **CI**: `run_build.json` calls `Tools/AFO/Build Putter Menu UI` — note the mismatch with the actual `MenuItem` name (`Rebuild` not `Build`); fix if CI fails.

## Test
- `com.unity.test-framework` (1.6.0) and `com.unity.test-framework.performance` are available.
- No project-specific test assemblies found yet.

## Conventions
- **Input**: Unity Input System (`InputSystem_Actions.inputactions`), keyboard + mouse only.
- **Code style**: `[Header("")]` on serialized fields, XML doc comments on classes, `_underscore` for private fields.
- **UI**: Built procedurally in `BuildPutterMenuUI.cs` — do not edit the scene directly; change the builder and rebuild.
- **Menu font**: Assets/Putter/Fonts/Oswald-Variable.ttf (display), fallback: LegacyRuntime.ttf.
- **Scene roles**: Putter = menu, Nate = gameplay, Kao = map editor scene.
- **MCP**: Uses `com.ivanmurzak.unity.mcp` (1.4.0) for AI-driven tool calls.
- Don't add `.sln`/`.csproj` files — they're in `.gitignore`.