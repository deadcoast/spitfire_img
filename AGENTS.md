# Repository Guidelines

## Project Structure & Module Organization

This repository currently contains planning material for `spitfire.img`, a Windows-native image viewer. The main source document is `spitfire_img.md`, which defines product vision, UX requirements, architecture direction, keyboard shortcuts, and MVP scope.

When implementation begins, keep app code and documentation separated:

```text
docs/                  Product, UX, architecture, and roadmap docs
src/ImageViewer.App/   WinUI 3 application code
tests/                 Unit and integration tests
assets/                Icons, sample images, and design references
```

Prefer small, topic-focused Markdown files under `docs/` instead of expanding one large planning document indefinitely.

## Build, Test, and Development Commands

There is no buildable application or test suite in this repository yet. For document-only changes, validate Markdown manually and keep links current.

Once the WinUI/.NET solution exists, expected commands should be documented here and in `README.md`, for example:

```powershell
dotnet build
dotnet test
dotnet format
```

Do not add placeholder scripts that do not run.

## Coding Style & Naming Conventions

For Markdown, use sentence-case headings, concise sections, and fenced code blocks with language labels where useful. Keep line wrapping readable and avoid duplicating requirements across files.

For future C# code, use standard .NET naming conventions: `PascalCase` for types, methods, and public properties; `camelCase` for locals and parameters; `Async` suffixes for asynchronous methods. Organize app code around the planned `Views`, `ViewModels`, `Services`, `Models`, and `Resources` modules described in `spitfire_img.md`.

## Testing Guidelines

No testing framework is configured yet. When implementation starts, add tests alongside the first non-trivial service logic, especially folder playlist scanning, sorting, filtering, settings persistence, and image metadata handling.

Use descriptive test names such as `OpenImage_LoadsParentFolderPlaylist` and keep UI-heavy behavior covered by integration or manual test notes when automation is impractical.

## Commit & Pull Request Guidelines

This directory is not currently a Git repository, so no project-specific commit history is available. Use short, imperative commit messages such as `Add MVP roadmap docs` or `Implement folder playlist service`.

Pull requests should include a clear summary, affected docs or modules, test results if code changed, and screenshots or short recordings for UI changes. Link related issues or decision records when available.

## Agent-Specific Instructions

Preserve the product direction in `spitfire_img.md`: presentation-first viewing, fading UI, automatic folder-as-playlist behavior, and WinUI 3 with Windows App SDK as the primary technical direction. Do not introduce editing, cataloging, or cloud features without updating the scope and rationale first.
