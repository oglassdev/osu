// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    /// <summary>
    /// Compose-style quick access toolbox buttons for storyboard editing tools.
    /// Icons remain visible when contracted; labels appear when the toolbox expands.
    /// </summary>
    internal partial class DesignToolbox : FillFlowContainer
    {
        private const float button_spacing = 5;

        public DesignToolbox()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, button_spacing);
        }

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            var spritesButton = createDialogTool(@"Sprites", @"Sprite library", FontAwesome.Solid.Images, () => new SpriteLibraryPanel());
            var addElementButton = createDialogTool(@"Add element", @"Add storyboard element", FontAwesome.Solid.Plus, () => new StoryboardElementCreationPanel());

            makeExclusive(spritesButton.Selected, addElementButton.Selected);
            spritesButton.Selected.BindValueChanged(_ => updateDialogVisibility(spritesButton, addElementButton));
            addElementButton.Selected.BindValueChanged(_ => updateDialogVisibility(spritesButton, addElementButton));

            var useSkinSprites = new BindableBool(beatmap.Storyboard.UseSkinSprites);
            useSkinSprites.BindValueChanged(value =>
            {
                if (beatmap.Storyboard.UseSkinSprites == value.NewValue)
                    return;

                beatmap.BeginChange();
                beatmap.Storyboard.UseSkinSprites = value.NewValue;
                beatmap.EndChange();
            });

            Children = new Drawable[]
            {
                createToolboxGroup(@"tools", spritesButton, addElementButton),
                createToolboxGroup(
                    @"view",
                    createBoolToggle(@"Background", FontAwesome.Solid.Image, state.ShowBackground),
                    createBoolToggle(@"Fail", FontAwesome.Solid.TimesCircle, state.ShowFail),
                    createBoolToggle(@"Pass", FontAwesome.Solid.CheckCircle, state.ShowPass),
                    createBoolToggle(@"Foreground", FontAwesome.Solid.LayerGroup, state.ShowForeground),
                    createBoolToggle(@"Prefer skin sprites", FontAwesome.Solid.Tshirt, useSkinSprites),
                    createBoolToggle(@"Preview passing state", FontAwesome.Solid.Eye, state.PreviewPassing)),
            };
        }

        private void updateDialogVisibility(DesignDialogToolButton spritesButton, DesignDialogToolButton addElementButton)
        {
            if (!spritesButton.Selected.Value && !addElementButton.Selected.Value)
                state.DialogHost?.HideDialog();
        }

        private static EditorToolboxGroup createToolboxGroup(string title, params Drawable[] children) =>
            new EditorToolboxGroup(title)
            {
                Spacing = new Vector2(0, button_spacing),
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, button_spacing),
                    Children = children,
                },
            };

        private static void makeExclusive(params BindableBool[] selections)
        {
            foreach (var selection in selections)
            {
                selection.BindValueChanged(change =>
                {
                    if (!change.NewValue)
                        return;

                    foreach (var other in selections.Where(s => s != selection))
                        other.Value = false;
                });
            }
        }

        private static DesignDialogToolButton createDialogTool(string label, string dialogTitle, IconUsage icon, Func<Drawable> createContent) =>
            new DesignDialogToolButton(label, dialogTitle, () => new SpriteIcon { Icon = icon }, createContent);

        private static DesignToolboxTernaryButton createBoolToggle(string label, IconUsage icon, BindableBool target)
        {
            var ternary = new BindableWithCurrent<TernaryState>(target.Value ? TernaryState.True : TernaryState.False);

            target.BindValueChanged(value => ternary.Value = value.NewValue ? TernaryState.True : TernaryState.False);
            ternary.BindValueChanged(value => target.Value = value.NewValue == TernaryState.True);

            return new DesignToolboxTernaryButton
            {
                Description = label,
                TooltipText = label,
                CreateIcon = () => new SpriteIcon { Icon = icon },
                Current = ternary,
            };
        }
    }
}
