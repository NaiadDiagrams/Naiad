# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Naiad is a .NET library that renders Mermaid diagram markup to SVG without requiring a browser or JavaScript runtime. Built on .NET 10.0 with C# latest, using Pidgin parser combinators for input parsing.

## Build & Test Commands

```bash
# Build
dotnet build src --configuration Release

# Run all tests (requires Playwright/Chromium installed)
dotnet test src --configuration Release

# Run a single test
dotnet test src --configuration Release --filter "FullyQualifiedName~PieTests.Simple"

# Install Playwright browsers (required before first test run)
dotnet exec --depsfile ./src/Tests/bin/Release/net10.0/Tests.deps.json --runtimeconfig ./src/Tests/bin/Release/net10.0/Tests.runtimeconfig.json ./src/Tests/bin/Release/net10.0/Microsoft.Playwright.dll install chromium
```

## Architecture

**Pipeline:** Each diagram type follows a three-stage pipeline: **Parser → Model → Renderer**

- **Parser** (`IDiagramParser<TModel>`): Converts Mermaid text to a model using Pidgin parser combinators. Parsers use LINQ-style `from`/`select` syntax. Shared parser building blocks live in `CommonParsers`.
- **Model** (`DiagramBase`): Domain objects. `GraphDiagramBase` extends this for node/edge diagrams (Flowchart, Class, ER, State, etc.) with `Node`, `Edge`, `Subgraph`.
- **Renderer** (`IDiagramRenderer<TModel>`): Converts model to `SvgDocument` using `SvgBuilder` fluent API.

**Entry point:** `Mermaid.Render(input, options?)` in `src/Naiad/Mermaid.cs` — auto-detects diagram type from first line, dispatches to the appropriate parser+renderer pair.

**Layout engine:** `DagreLayoutEngine` in `src/Naiad/Layout/` implements Sugiyama-style graph layout (used by Flowchart and similar graph-based diagrams).

**Key directories:**
- `src/Naiad/Diagrams/` — one subfolder per diagram type, each containing parser and renderer
- `src/Naiad/Layout/` — Dagre-based graph layout algorithm
- `src/Naiad/Models/` — shared data models
- `src/Naiad/Rendering/` — SVG element types (`SvgBuilder`, `SvgDocument`, `SvgGroup`, `SvgPath`, etc.) and `MermaidStyles`

## Testing

Tests use NUnit + Playwright + Verify (snapshot testing). Each diagram type has a test folder under `src/Tests/`.

- `TestBase` (in `SvgVerify.cs`) renders SVG, then uses Playwright to screenshot it as PNG for visual regression via Verify.ImageSharp.Compare.
- `DocGeneratorTests` auto-generates markdown test renders in `src/test-renders/`.
- When tests fail, `.received.*` files are created alongside `.verified.*` files — review and accept with your Verify diff tool.

## Code Style

- Strict `.editorconfig` with `TreatWarningsAsErrors: true` and `EnforceCodeStyleInBuild`
- File-scoped namespaces, expression-bodied members, `var` everywhere
- LF line endings, UTF-8, 4-space indent for C#
- Central package management via `src/Directory.Packages.props`
