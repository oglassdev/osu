// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Screens.Edit.Components;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class DesignEditorLayout : CompositeDrawable
    {
        private const float top_panel_height = 80;
        private const float bottom_panel_height = 150;

        public DesignEditorLayout()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, top_panel_height),
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, bottom_panel_height),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new DesignPanel(DesignPanel.PanelStyle.Toolbar)
                        {
                            Name = @"Top panel",
                        },
                    },
                    new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Absolute, EditorSidebar.WIDTH),
                                new Dimension(),
                                new Dimension(GridSizeMode.Absolute, EditorSidebar.WIDTH),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new EditorSidebar
                                    {
                                        Name = @"Left panel",
                                    },
                                    new DesignCanvas
                                    {
                                        Name = @"Canvas",
                                    },
                                    new EditorSidebar
                                    {
                                        Name = @"Right panel",
                                    },
                                },
                            },
                        },
                    },
                    new Drawable[]
                    {
                        new DesignPanel(DesignPanel.PanelStyle.Toolbar)
                        {
                            Name = @"Bottom panel",
                        },
                    },
                },
            };
        }
    }
}
