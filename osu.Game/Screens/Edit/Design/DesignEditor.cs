// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Design.Components;
using osuTK;

namespace osu.Game.Screens.Edit.Design
{
    /// <summary>
    /// Top-level container for storyboard editing.
    /// Houses the canvas and transformation timeline between Compose-style editor toolboxes.
    /// </summary>
    internal partial class DesignEditor : CompositeDrawable
    {
        private const float properties_width = 250;
        private const float timeline_height = 190;

        public DesignEditor()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [Resolved]
        private DesignStoryboardState storyboardState { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            var dialogHost = new DesignToolDialogHost();
            storyboardState.DialogHost = dialogHost;

            InternalChildren =
            [
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding
                    {
                        Left = HitObjectComposer.TOOLBOX_CONTRACTED_SIZE_LEFT,
                        Right = properties_width,
                        Bottom = timeline_height,
                    },
                    Child = new DesignStoryboardCanvas(),
                },
                new Container
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativeSizeAxes = Axes.X,
                    Height = timeline_height,
                    Padding = new MarginPadding
                    {
                        Left = HitObjectComposer.TOOLBOX_CONTRACTED_SIZE_LEFT,
                        Right = properties_width,
                    },
                    Child = new TransformationTimeline(),
                },
                new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = colourProvider.Background5,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new ExpandingToolboxContainer(HitObjectComposer.TOOLBOX_CONTRACTED_SIZE_LEFT, 200)
                        {
                            Child = new DesignToolbox(),
                        },
                    },
                },
                createPropertiesPanel(
                    colourProvider,
                    new StoryboardObjectList(),
                    new CommandToolbar(),
                    new DesignReadingsPanel()),
                dialogHost,
            ];
        }

        private static Drawable createPropertiesPanel(OverlayColourProvider colourProvider, params Drawable[] children)
        {
            return new Container
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                RelativeSizeAxes = Axes.Y,
                Width = properties_width,
                Children =
                [
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new OsuScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ScrollbarOverlapsContent = false,
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding { Vertical = 5 },
                            Spacing = new Vector2(0, 5),
                            Children = children,
                        },
                    },
                ],
            };
        }

    }
}
