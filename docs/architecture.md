# NICK AI — Architecture

## 1. Projects

```
NickAI.Core        (net8.0)         domain + providers + agents + services
NickAI.App         (net8.0-windows) WPF shell, MVVM, themes, views
NickAI.Core.Tests  (net8.0)         xUnit tests for the core
```

`NickAI.Core` has no WPF dependency, so agents, routing and stores can be unit
tested without a UI. `NickAI.Core.Tests` also runs on macOS/Linux, where the WPF
shell can only be type-checked (`-p:EnableWindowsTargeting=true`).

## 2. Request flow

```
user text
   │
   ▼
RequestClassifier  ──►  capability chain (e.g. ["code"])
   │
   ▼
ChatService
   ├─ chain == ["chat"]  ──►  stream directly from IModelProvider
   └─ otherwise          ──►  OrchestratorAgent
                                 │
                                 ├─ Plan (steps = chain)
                                 ├─ AgentRegistry.TryGet(capability)
                                 │     └─ missing → step marked Failed (honest message)
                                 └─ IAgent.ExecuteAsync(...)
                                         │
                                         └─ writes Artifacts + reports progress
```

Progress and deltas are surfaced to the UI as `ChatStreamEvent`s
(`Activity`, `Delta`, `Artifact`, `Plan`, `Error`, `Completed`). Activity strings
are concise user-facing messages — never chain-of-thought.

## 3. Key abstractions

| Abstraction | Responsibility |
| --- | --- |
| `IModelProvider` | A source of models; streaming and non-streaming chat |
| `ModelManager` | Registered providers, aggregated model list, active model |
| `ModelRouter` | Picks a model for a capability using honest capability flags |
| `IAgent` | A specialized worker owning one capability key |
| `AgentRegistry` | Capability → agent map; missing capabilities are reported |
| `OrchestratorAgent` | Plans and dispatches steps, collects artifacts |
| `IArtifactStore` | Artifacts backed by real files |
| `IProjectStore` | Projects, memory and technology detection |
| `IPermissionService` | ALLOW / ASK / DENY gate |
| `IProcessRunner` | External commands with timeout and captured output |

## 4. Adding a capability agent

1. Add a capability key in `Capabilities`.
2. Implement `IAgent` (`Name`, `Capability`, `ExecuteAsync`).
3. Register it in `AgentRegistry` in `App.xaml.cs`.
4. Add classifier keywords in `RequestClassifier` if the capability needs routing.

Until step 3 is done, the orchestrator reports the capability as not implemented.

## 5. EXE and installer pipeline

```
scripts/build.ps1            dotnet restore + build + dotnet test
scripts/publish.ps1          dotnet publish -r win-x64 --self-contained true
                             → publish/win-x64/NICKAI.exe
scripts/package-installer.ps1  ISCC installer/NickAI.iss
                             → publish/installer/NICK_AI_Setup.exe
```

The EXE is produced only by `dotnet publish`; `publish.ps1` fails if the expected
file is missing. The installer is produced only by Inno Setup; `package-installer.ps1`
fails if `ISCC.exe` is not installed.

## 6. Data locations

| Path | Contents |
| --- | --- |
| `%LOCALAPPDATA%\NickAI\artifacts` | Generated artifact files |
| `%LOCALAPPDATA%\NickAI\projects.json` | Projects and project memory |

## 7. Roadmap

Ordered by value to the product:

1. **Office generation agents** — `PresentationAgent` (PPTX), `SpreadsheetAgent`
   (XLSX), `DocumentAgent` (DOCX) and `PDFAgent`, using validated OOXML/PDF
   libraries, each producing a real artifact file.
2. **Local knowledge / RAG** — import PDF/DOCX/MD/code, chunk, embed with an
   embedding model, retrieve into context.
3. **Memory** — conversation, project and user-approved long-term memory.
4. **Browser agent** — Playwright-based navigation and structured page extraction.
5. **Computer agent** — window focus, keyboard/mouse, screen capture behind
   permission prompts.
6. **Data agent** — CSV/XLSX/JSON analysis with charts.
7. **Project EXE builder** — package compatible user projects end to end.
8. **Publishing agents** — with an explicit confirmation step showing exactly what
   would be uploaded.
9. **Installer first-run experience** — environment detection, Ollama setup,
   permissions wizard.
