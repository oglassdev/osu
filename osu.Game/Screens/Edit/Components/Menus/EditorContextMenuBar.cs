// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Components.Menus
{
    internal partial class EditorContextMenuBar : VisibilityContainer
    {
        private readonly Box background;
        private readonly Circle indicator;
        private readonly EditorContextMenu menu;

        public IReadOnlyList<MenuItem> Items { get; private set; } = Array.Empty<MenuItem>();

        protected override bool StartHidden => true;

        public EditorContextMenuBar()
        {
            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 40,
                            Child = indicator = new Circle
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(8),
                            },
                        },
                        menu = new EditorContextMenu
                        {
                            RelativeSizeAxes = Axes.Y,
                            MaxHeight = 600,
                        },
                    },
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            background.Colour = colourProvider.Background5;
            indicator.Colour = colourProvider.Highlight1;
        }

        public void SetItems(IReadOnlyList<MenuItem> items)
        {
            Items = items;
            menu.Items = items;

            if (items.Count > 0)
                Show();
            else
                Hide();
        }

        protected override void PopIn() => this.FadeIn();

        protected override void PopOut() => this.FadeOut();
    }
}
