---
version: alpha
name: spitfire.img
description: A Windows image viewport with quiet, stationary controls.
colors:
  canvas: "#151515"
  text: "#FFFFFF"
  primary: "#8AB4F8"
  surface: "#272727"
typography:
  body:
    fontFamily: "Segoe UI Variable Text"
  title:
    fontFamily: "Segoe UI Variable Display"
omitted:
  - section: spacing
    reason: Canonical values remain in Resources/Styles/LayoutTokens.xaml.
  - section: rounded
    reason: Native WinUI controls and the canonical XAML style dictionaries own shapes.
  - section: components
    reason: Component ownership and native states are documented below.
---

# spitfire.img design

## Overview

An image viewing window for browsing local folders and inspecting pixels. The visual reference is a photographic viewing surface: the image has priority, with instruments at the edges. This is a desktop tool, with English UI and Unicode filenames. No market-specific behavior is assumed.

The distinctive behavior is the disappearance of controls without movement or viewport resize. Floating dashboard cards, large headings over images, repeated status badges and decorative entrance animation conflict with that purpose.

Product authority: [roadmap](docs/roadmap.md), phase requirements, and the [current repair](docs/phase-5-professional-ui-qol/viewer-repair-20261019.md).

## Colors

Existing XAML theme dictionaries remain canonical. Frontmatter mirrors dark `ViewerCanvasBackgroundBrush`, `ViewerTextPrimaryBrush`, `ViewerAccentBrush` and the RGB of `ViewerChromeSurfaceActiveBrush`. Light theme retains equivalent semantic roles. The image is never tinted by the chrome. Selection and focus use accent; warnings retain distinct text and semantic colors.

## Typography

Use the installed Segoe UI Variable family through `TextStyles.xaml`; native controls inherit Windows text and scaling behavior. Filenames and numeric metadata remain legible, truncate in constrained width, and expose full values through tooltips. Avoid stacked captions for ordinary filename, count and dimensions.

## Layout

One image viewport. A compact stationary toolbar at the top and a single-line status band at the bottom overlay it without changing its fit dimensions. Optional thumbnails sit above the status band or below the toolbar. Empty view has an actual Open image action.

At smaller window widths optional folder/mode text yields space before primary actions. Advanced functions remain accessible through menus. No visual state moves controls as a side effect of pointer activity.

## Elevation & Depth

Edge chrome uses a flat surface and one separator. Native menu flyouts and dialogs provide transient elevation. Metadata, search, comparison and layout controls appear only when explicitly requested.

## Shapes

Chrome meets the window edges with square corners. Native control radius and existing overlay styles remain canonical. Do not wrap each status value in a card.

## Components

| Capability | Canonical owner | Contract |
|---|---|---|
| Toolbar and activity | FadingChrome | Idle opacity zero; repeated activity only resets deadline; menu, pointer and keyboard interaction hold visibility |
| Commands | MainViewModel command registry | Menus and palette dispatch the same registered actions; native file dialogs remain MainWindow-owned |
| Image viewport | ImageCanvas and viewport geometry service | Fit, physical pixel size, zoom, bounded pan; GIF frames never fade |
| Status | BottomInfoBar | Filename, index, dimensions and zoom on one line |
| Search/list selection | WinUI TextBox/ListView in existing overlays | Native semantics, explicit clear and Escape; restore viewer focus |
| Dialogs and menus | WinUI ContentDialog/MenuFlyout | Platform popup geometry and focus ownership |
| Scrollbars | WinUI ScrollViewer theme | Platform-owned visible/operable scrollbars, no web styling layer |
| Errors | MainWindow InfoBar | Stable overlay, useful recovery action, no silent image presentation failure |

Token path: `Resources/ThemeTokens` and `Resources/Styles` → `App.xaml` merged dictionaries → named shared view components. This file explains intent; it does not generate a second token system.

Native buttons provide hover, pressed, disabled and keyboard focus states. Every icon action has a tooltip and accessible name. Unavailable image actions are disabled. Overwrite still requires the existing confirmation.

Chrome uses opacity only. Repeated activity does not restart reveal. Shared motion replaces previous animation and cancels its completion callback. Reduced-motion preference resolves transitions immediately.

## Do's and Don'ts

- Keep image coordinates stable when controls appear or disappear.
- Make implemented functionality discoverable without memorizing shortcuts.
- Verify rendered behavior; a named XAML element is not evidence of usability.
- Do not fade GIF frames, slide chrome on mouse movement, or add status cards without a viewing task.
