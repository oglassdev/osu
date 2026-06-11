// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Overlays;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class DesignPanel : Container
    {
        public enum PanelStyle
        {
            Sidebar,
            Toolbar,
        }

        private readonly Box background;
        private readonly PanelStyle style;

        protected override Container<Drawable> Content { get; }

        public DesignPanel(PanelStyle style = PanelStyle.Sidebar)
        {
            this.style = style;
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                Content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            background.Colour = style == PanelStyle.Toolbar ? colourProvider.Background6 : colourProvider.Background5;
        }
    }
}
