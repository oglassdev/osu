# Design mode (storyboard editor)

Guidance for work under `Screens/Edit/Design/`. Build and test via `osu.Desktop.slnf` → this project / `osu.Game.Tests`.

This is **Design mode** (`EditorScreenMode.Design`, F2) — the in-editor home for storyboard editing. Not to be confused with `Screens/Edit/Setup/DesignSection.cs`, which only covers beatmap-wide design *settings* (countdown, widescreen, etc.).

---

## Storyboard domain

The editor manipulates the in-memory `Storyboard` on `EditorBeatmap`. Understand the legacy format and runtime model before building UI around it.

### Wiki (behaviour spec)

- [Storyboard scripting](https://osu.ppy.sh/wiki/en/Storyboard/Scripting) — overview
- [General rules](https://osu.ppy.sh/wiki/en/Storyboard/Scripting/General_Rules) — layers, pass/fail state, screen size (640×480), draw order
- [Objects](https://osu.ppy.sh/wiki/en/Storyboard/Scripting/Objects) — sprite/animation declarations
- [Commands](https://osu.ppy.sh/wiki/en/Storyboard/Scripting/Commands) — F/M/S/R/C and advanced commands
- [Compound commands](https://osu.ppy.sh/wiki/en/Storyboard/Scripting/Compound_Commands) — loops, triggers

The wiki describes `.osb`/`.osu` scripting. The editor should produce the same semantics through the C# model and encoders.

### Code model (read these first)

| Area | Path |
|------|------|
| Root model | `Storyboards/Storyboard.cs` |
| Layers & elements | `StoryboardLayer.cs`, `StoryboardSprite.cs`, `StoryboardAnimation.cs`, … |
| Commands | `Storyboards/Commands/` |
| Runtime drawables | `Storyboards/Drawables/` |
| Decode / encode | `Beatmaps/Formats/LegacyStoryboardDecoder.cs`, `LegacyStoryboardEncoder.cs` |
| Editor integration | `Screens/Edit/EditorBeatmap.cs` (`Storyboard` property), `BeatmapEditorChangeHandler.cs` (save) |
| Preview | `Screens/Backgrounds/EditorBackgroundScreen.cs`, `Graphics/Backgrounds/BeatmapBackgroundWithStoryboard.cs` |

Layers are fixed in `Storyboard`’s constructor (`Background`, `Fail`, `Pass`, `Foreground`, `Overlay`, `Video`). Pass/fail visibility and z-order follow the wiki — preserve that when exposing layer UI.

---

## UI components

Reuse existing editor and framework primitives. Prefer composition over new base classes.

### Screen shell

Design mode is an `EditorScreen`. If you need a timeline band above the main area, extend `EditorScreenWithTimeline` (see Compose) and override `CreateMainContent` / `CreateTimelineContent`:

```csharp
// EditorScreenWithTimeline.cs — grid: timeline row + main content row
protected abstract Drawable CreateMainContent();
protected virtual Drawable CreateTimelineContent() => new Container();
```

`ComposeScreen` is the reference implementation: it async-loads content, wraps it in `EditorSkinProvidingContainer`, and wires a `TimelineBlueprintContainer` in the timeline slot.

### Top-level layout

Follow the Compose / Skin editor pattern: **left toolbox · centre canvas · right properties · bottom timeline**.

Compose defines toolbox widths as constants on `HitObjectComposer`:

```csharp
public const float TOOLBOX_CONTRACTED_SIZE_LEFT = 60;
public const float TOOLBOX_CONTRACTED_SIZE_RIGHT = 120;
```

A typical layout pads the canvas to reserve those regions (see `HitObjectComposer` and `SkinEditor` for `ExpandingToolboxContainer` usage). Use `OverlayColourProvider.Background5` behind toolbars/panels (`Box` fill).

### Form controls (properties / side panels)

For labelled inputs in scrollable side panels, use **UserInterfaceV2** form controls — same family as Setup sections:

```csharp
// Screens/Edit/Setup/DesignSection.cs
EnableCountdown = new FormCheckBox
{
    Caption = EditorSetupStrings.EnableCountdown,
    HintText = EditorSetupStrings.CountdownDescription,
    Current = { Value = Beatmap.Countdown != CountdownType.None },
},
```

Available controls live in `Graphics/UserInterfaceV2/` (`FormButton`, `FormTextBox`, `FormSliderBar`, `FormDropdown`, `FormCheckBox`, …). Visual tests: `osu.Game.Tests/Visual/UserInterface/TestSceneFormControls.cs`.

Setup sections wrap forms in `SetupSection` + `SectionHeader` + `FillFlowContainer`. For Design mode side panels, a plain `OsuScrollContainer` → `FillFlowContainer` is usually enough — you do not need to inherit `SetupSection` unless the UI is truly a setup-style form.

### Toolbox buttons

Compose toolbars use `EditorToolboxGroup` and ternary/radio buttons under `Screens/Edit/Components/`. Example:

```csharp
// Screens/Edit/Components/TernaryButtons/SampleBankTernaryButton.cs
public partial class SampleBankTernaryButton : CompositeDrawable
{
    [BackgroundDependencyLoader]
    private void load()
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        // ...
    }
}
```

Design-specific toolbox pieces belong in `Screens/Edit/Design/Components/` and should mirror this sizing (`RelativeSizeAxes = Axes.X`, `AutoSizeAxes = Axes.Y`).

### Selection & canvas interaction

For draggable/selectable elements, study Compose blueprints and selection handlers:

- `Screens/Edit/Compose/Components/SelectionHandler.cs` — selection box, bindable `SelectedItems`
- `Screens/Edit/Compose/Components/SelectionScaleHandler.cs`, `SelectionRotationHandler.cs`
- `Overlays/SkinEditor/SkinSelectionHandler.cs` — same idea outside Compose

Skin editor is the closest analogue for “position things on screen” editing (`SkinBlueprintContainer`, `SkinBlueprint`).

### Dependency injection

Resolve shared editor services rather than passing them through long constructor chains:

```csharp
[Resolved]
private EditorBeatmap EditorBeatmap { get; set; } = null!;

[BackgroundDependencyLoader]
private void load(OverlayColourProvider colourProvider) { ... }
```

Cache screen-scoped state with `[Cached]` on the owning screen or a dedicated state class if multiple siblings need it.

### Strings

User-facing text goes in `Localisation/EditorStrings.cs` (or a dedicated strings class if the surface area grows). Use `LocalisableString` on captions, not raw literals.

---

## File structure

Keep the screen entry point thin; push UI into `Components/`.

```
Screens/Edit/Design/
  DesignScreen.cs              # EditorScreen / EditorScreenWithTimeline subclass; mode entry
  DesignEditor.cs              # (optional) top-level layout container
  DesignStoryboard*.cs         # canvas, state, operations — co-locate if tightly coupled
  Components/
    DesignToolbox.cs           # left toolbar
    …                          # panels, timelines, overlays, dialogs
```

**Placement rules:**

- Screen-level types directly under `Screens/Edit/Design/`.
- Reusable UI widgets under `Screens/Edit/Design/Components/`.
- Do **not** put storyboard model types here — they live in `Storyboards/`.
- Shared editor widgets used across modes stay in `Screens/Edit/Components/` (only add there if Compose/Timing/etc. would also use it).
- Visual tests: `osu.Game.Tests/Visual/Editing/TestSceneDesignScreen.cs` (create/extend alongside the screen).

**Patterns to copy from:**

| Pattern | Location |
|---------|----------|
| Screen + timeline | `Screens/Edit/Compose/ComposeScreen.cs` |
| Toolbox + playfield layout | `Rulesets/Edit/HitObjectComposer.cs` |
| Overlay editor layout | `Overlays/SkinEditor/SkinEditor.cs` |
| Form-based sections | `Screens/Edit/Setup/SetupSection.cs` |
| External-edit workflow (today’s storyboard path) | `Screens/Edit/ExternalEditScreen.cs` |

---

## UI style guidelines

These are defaults, not hard rules. The storyboard editor is complex — deviate when a simpler or more specialised control is clearer.

- **Match the editor chrome.** Reuse `OverlayColourProvider` backgrounds, `OsuScrollContainer`, and toolbox dimensions from Compose. The editor should feel like one app, not a separate tool.
- **Prefer framework layout containers.** `GridContainer` for major regions, `FillFlowContainer` for stacked form fields, `Container` + padding for reserved toolbox/timeline space.
- **Use Form V2 for data entry.** Sliders, dropdowns, and checkboxes in side panels should use `Form*` controls unless the interaction is inherently graphical (e.g. dragging on the canvas).
- **Keep the canvas uncluttered.** Dense controls belong in toolbars and side panels; the centre is for the storyboard preview and direct manipulation.
- **Animate sparingly.** `FadeInFromZero(300, Easing.OutQuint)` matches other editor transitions; don’t add motion unless it aids orientation.
- **Complexity is allowed.** Multi-row timelines, modal tool dialogs, and custom overlays are fine when forms don’t fit — look at `Screens/Edit/Compose/Components/Timeline/` and `Overlays/SkinEditor/` for precedent, not limits.
