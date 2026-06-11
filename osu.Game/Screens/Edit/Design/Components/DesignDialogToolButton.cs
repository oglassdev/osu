// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Graphics.Cursor;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Design;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Design.Components
{
    /// <summary>
    /// A toolbox button that opens a <see cref="DesignToolDialog"/> instead of a narrow anchored popover.
    /// </summary>
    internal partial class DesignDialogToolButton : OsuButton, IHasTooltip
    {
        public BindableBool Selected { get; } = new BindableBool();

        private readonly Func<Drawable> createContent;
        private readonly LocalisableString dialogTitle;
        private readonly LocalisableString tooltipText;

        private Color4 defaultBackgroundColour;
        private Color4 defaultIconColour;
        private Color4 selectedBackgroundColour;
        private Color4 selectedIconColour;

        private Drawable icon = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        public DesignDialogToolButton(LocalisableString text, LocalisableString dialogTitle, Func<Drawable> createIcon, Func<Drawable> createContent)
        {
            Text = text;
            this.dialogTitle = dialogTitle;
            this.createContent = createContent;
            tooltipText = text;

            RelativeSizeAxes = Axes.X;

            Add(icon = createIcon().With(b =>
            {
                b.Blending = BlendingParameters.Additive;
                b.Anchor = Anchor.CentreLeft;
                b.Origin = Anchor.CentreLeft;
                b.Size = new Vector2(20);
                b.X = 10;
            }));
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            defaultBackgroundColour = colourProvider.Background3;
            selectedBackgroundColour = colourProvider.Background1;

            defaultIconColour = defaultBackgroundColour.Darken(0.5f);
            selectedIconColour = selectedBackgroundColour.Lighten(0.5f);

            Action = () => state.DialogHost?.Show(dialogTitle, createContent, Selected);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Selected.BindValueChanged(_ => updateSelectionState(), true);
        }

        private void updateSelectionState()
        {
            if (!IsLoaded)
                return;

            BackgroundColour = Selected.Value ? selectedBackgroundColour : defaultBackgroundColour;
            icon.Colour = Selected.Value ? selectedIconColour : defaultIconColour;
        }

        protected override SpriteText CreateText() => new ExpandableSpriteText
        {
            Depth = -1,
            Origin = Anchor.CentreLeft,
            Anchor = Anchor.CentreLeft,
            X = 40f,
        };

        public LocalisableString TooltipText => tooltipText;
    }
}
