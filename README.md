# NICK AI

Universal local AI creation, development and computer agent for Windows 10/11.

Developer: **Yoga Kumar**

NICK AI is a desktop workspace (not a chatbot) that understands a natural-language
request, plans it, runs it through specialized agents, and writes **real files** to
disk. It runs local models through [Ollama](https://ollama.com) — no cloud account
is required for the core experience.

---

## Status of this build (read this first)

This repository is the **foundation** of the desktop app. It is real, compiling
C# — but it is not the complete 41-section specification yet. What is actually
implemented and what is not:

### Implemented

| Area | What works |
| --- | --- |
| Shell | WPF dark/light/system theme, top bar, sidebar navigation, resizable layout |
| Chat | Streaming responses from local models, markdown/code-aware rendering, activity log, stop |
| Models | `OllamaModelProvider` (real HTTP), model list from `/api/tags`, capabilities from `/api/show` |
| Routing | Deterministic request classifier → capability → agent routing |
| Agents | `ChatAgent`, `CodeAgent` (writes real code files), `DocumentationAgent` (Markdown), `BuildAgent` (runs the real build) |
| Artifacts | Every output is an `Artifact` with a real file on disk, live artifact list, open/reveal |
| Projects | Add a folder, technology detection, project-scoped artifacts and build logs |
| Permissions | ALLOW / ASK / DENY per category with a permission center UI |
| Terminal | `ProcessRunner` with timeout, cancellation, exit code and captured stdout/stderr |
| Tests | 161 xUnit tests over classification/routing, parsing, permissions, orchestration and the file-based stores |

### Not implemented yet

The following spec areas are **not** in this build, and NICK will tell you so
instead of pretending:

- Presentation (PPTX), spreadsheet (XLSX), Word (DOCX) and PDF generation
- Image, video and audio generation/editing
- Browser agent (Playwright) and computer agent (mouse/keyboard/screen)
- Local knowledge / RAG, embeddings, long-term memory
- Game engine integration (Unity/Godot/Unreal)
- Installer generation for *other* projects, and publishing/deployment agents

These are tracked in `docs/architecture.md` under "Roadmap".

---

## Requirements

- **Windows 10 or 11**
- **.NET 8 SDK** (for building) — <https://dotnet.microsoft.com/download>
- **Ollama** running locally on `http://localhost:11434` with at least one model
  pulled, e.g. `ollama pull llama3.1`
- **Inno Setup 6** (only if you want to build `NICK_AI_Setup.exe`)

## Build and run

```powershell
# Build the solution
pwsh ./scripts/build.ps1 -Configuration Release

# Run from the build output
dotnet run --project src/NickAI.App/NickAI.App.csproj
```

`build.ps1` also runs the test suite after a successful compile (pass `-SkipTests` to
skip it).

## Tests

`tests/NickAI.Core.Tests` covers the deterministic core, which is where routing and
honesty guarantees live:

| Suite | Covers |
| --- | --- |
| `RequestClassifierTests` | Every "Create a …" request from the spec routes to the right capability |
| `OrchestratorAgentTests` | Planning, agent dispatch, artifact collection, unimplemented capabilities reported as failed |
| `ModelRoutingTests` | Provider refresh and capability-based model selection |
| `MessageContentParserTests` | Prose vs. fenced code, including partial fences while streaming |
| `PermissionServiceTests` | ALLOW / ASK / DENY defaults and remembered choices |
| `CapabilitiesTests` | Activity labels and model capability flags |
| `FileArtifactStoreTests` | Real files on disk: CRUD, hostile file names, name collisions, concurrent writers |
| `FileProjectStoreTests` | `projects.json` round-trips, corrupt index recovery, technology detection |

Each file-based test runs against its own temporary directory (`TempDirectoryTest`) and
removes it afterwards, including on failure.

```powershell
dotnet test tests/NickAI.Core.Tests/NickAI.Core.Tests.csproj
```

The suite targets `net8.0` only, so it runs on any OS without the WPF shell. On
macOS/Linux the shell can still be type-checked with
`dotnet build NickAI.sln -p:EnableWindowsTargeting=true`.

## Produce NICKAI.exe

```powershell
# Self-contained (bundles the .NET runtime) — recommended for distribution
pwsh ./scripts/publish.ps1 -Runtime win-x64

# Framework-dependent (smaller, requires the .NET 8 Desktop Runtime)
pwsh ./scripts/publish.ps1 -Runtime win-x64 -FrameworkDependent

# Windows on ARM
pwsh ./scripts/publish.ps1 -Runtime win-arm64
```

The published application is written to `publish/<runtime>/NICKAI.exe`. The script
fails loudly if `dotnet publish` does not produce it — it never fabricates an EXE.

## Produce NICK_AI_Setup.exe

```powershell
pwsh ./scripts/package-installer.ps1 -Runtime win-x64
```

Requires Inno Setup 6. Builds the installer from `installer/NickAI.iss`, including
a Start Menu shortcut, an optional desktop shortcut, an uninstaller and version
metadata.

## Repository layout

```
NickAI.sln
src/
  NickAI.Core/      UI-free domain: models, providers, agents, services, stores
  NickAI.App/       WPF desktop shell (MVVM), themes, views
tests/
  NickAI.Core.Tests/  xUnit tests for the UI-free core
scripts/            build / publish / package PowerShell scripts
installer/          Inno Setup definition
docs/               requirements and architecture
```

## Where NICK stores data

Everything local lives under `%LOCALAPPDATA%\NickAI`:

- `artifacts/` — every generated file
- `projects.json` — projects and their memory

Nothing is uploaded anywhere by this build.

## Extending NICK

- **Add a model provider**: implement `IModelProvider` and register it in `App.xaml.cs`.
- **Add an agent**: implement `IAgent` with a capability key from
  `Capabilities`, then register it in the `AgentRegistry` in `App.xaml.cs`.
  A capability with no agent is reported as unsupported, never faked.
