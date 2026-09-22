# NICK AI — Requirements

## 1. Purpose

NICK AI is a Windows desktop AI workspace that turns natural-language requests
into real deliverables: code, documents, artifacts and builds. It is an AI
creation and development environment, not a chat window with extra styling.

## 2. Primary user stories

| # | Request | Expected outcome |
| --- | --- | --- |
| U1 | "Explain what a WebSocket is." | Streamed answer from a local model, no tools |
| U2 | "Write a C# script that renames files." | Real `.cs` file created as an Artifact |
| U3 | "Build my project into an EXE." | Real build command run, real output, exit code reported |
| U4 | "Create a PowerPoint about AI." | *Not implemented* — NICK must say so explicitly |
| U5 | "Document this module." | Real Markdown file created as an Artifact |

## 3. Functional requirements

- **FR1** Local-first: core experience works against a local Ollama runtime.
- **FR2** Every generated output is an Artifact with a real file path.
- **FR3** Requests are classified and routed to a capability-specific agent.
- **FR4** Capabilities without an agent must be reported as unsupported.
- **FR5** Builds run through a real process runner with timeout, cancellation,
  exit code and captured stdout/stderr.
- **FR6** High-impact actions are gated by a permission center (ALLOW/ASK/DENY).
- **FR7** The UI never shows private chain-of-thought — only concise activity.
- **FR8** Model capabilities are reported only when the runtime reports them or
  conservative name heuristics support them.

## 4. Non-functional requirements

- **NFR1** .NET 8, WPF, MVVM, `net8.0-windows`.
- **NFR2** Core logic lives in a UI-free library so it can be unit tested.
- **NFR3** Dark, light and system themes.
- **NFR4** Keyboard-friendly: Ctrl+Enter sends, Enter inserts a newline.
- **NFR5** No fabricated results: no fake EXEs, no claimed success without a
  verified artifact.
- **NFR6** Data stays local under `%LOCALAPPDATA%\NickAI`.

## 5. Out of scope for this milestone

Presentation/spreadsheet/document/PDF generation, image/video/audio generation,
browser and computer agents, RAG/memory, game engine integration, publishing
agents. See `docs/architecture.md` for the roadmap.
