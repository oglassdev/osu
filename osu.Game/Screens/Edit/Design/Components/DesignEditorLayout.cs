// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class DesignEditorLayout : CompositeDrawable
    {
        private const float default_side_panel_width = 200;
        private const float default_top_panel_height = 80;
        private const float default_bottom_panel_height = 150;

        private readonly BindableFloat topPanelHeight = new BindableFloat(default_top_panel_height);
        private readonly BindableFloat bottomPanelHeight = new BindableFloat(default_bottom_panel_height);
        private readonly BindableFloat leftPanelWidth = new BindableFloat(default_side_panel_width);
        private readonly BindableFloat rightPanelWidth = new BindableFloat(default_side_panel_width);

        private GridContainer mainGrid = null!;
        private GridContainer middleGrid = null!;

        private DesignResizeHandle topResizeHandle = null!;
        private DesignResizeHandle bottomResizeHandle = null!;
        private DesignResizeHandle leftResizeHandle = null!;
        private DesignResizeHandle rightResizeHandle = null!;

        public DesignEditorLayout()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                mainGrid = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
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
                            middleGrid = new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        new DesignPanel
                                        {
                                            Name = @"Left panel",
                                        },
                                        new DesignCanvas
                                        {
                                            Name = @"Canvas",
                                        },
                                        new DesignPanel
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
                },
                topResizeHandle = createResizeHandle(DesignResizeHandle.HandleDirection.Horizontal, topPanelHeight),
                bottomResizeHandle = createResizeHandle(DesignResizeHandle.HandleDirection.Horizontal, bottomPanelHeight, invertDelta: true),
                leftResizeHandle = createResizeHandle(DesignResizeHandle.HandleDirection.Vertical, leftPanelWidth),
                rightResizeHandle = createResizeHandle(DesignResizeHandle.HandleDirection.Vertical, rightPanelWidth, invertDelta: true),
            };

            topPanelHeight.BindValueChanged(_ => updateGridDimensions(), true);
            bottomPanelHeight.BindValueChanged(_ => updateGridDimensions(), true);
            leftPanelWidth.BindValueChanged(_ => updateGridDimensions(), true);
            rightPanelWidth.BindValueChanged(_ => updateGridDimensions(), true);
        }

        protected override void Update()
        {
            base.Update();

            float middleHeight = Math.Max(0, DrawHeight - topPanelHeight.Value - bottomPanelHeight.Value);

            topResizeHandle.Position = new Vector2(0, topPanelHeight.Value);
            topResizeHandle.Width = DrawWidth;

            bottomResizeHandle.Position = new Vector2(0, DrawHeight - bottomPanelHeight.Value);
            bottomResizeHandle.Width = DrawWidth;

            leftResizeHandle.Position = new Vector2(leftPanelWidth.Value, topPanelHeight.Value);
            leftResizeHandle.Height = middleHeight;

            rightResizeHandle.Position = new Vector2(DrawWidth - rightPanelWidth.Value, topPanelHeight.Value);
            rightResizeHandle.Height = middleHeight;
        }

        private void updateGridDimensions()
        {
            mainGrid.RowDimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, topPanelHeight.Value),
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, bottomPanelHeight.Value),
            };

            middleGrid.ColumnDimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, leftPanelWidth.Value),
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, rightPanelWidth.Value),
            };
        }

        private static DesignResizeHandle createResizeHandle(DesignResizeHandle.HandleDirection direction, BindableFloat targetSize, bool invertDelta = false)
        {
            var handle = new DesignResizeHandle
            {
                Direction = direction,
                InvertDelta = invertDelta,
            };

            handle.TargetSize.BindTarget = targetSize;
            return handle;
        }
    }
}
