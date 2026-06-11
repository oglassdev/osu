// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Overlays;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class DesignResizeHandle : CompositeDrawable
    {
        public enum HandleDirection
        {
            Horizontal,
            Vertical,
        }

        public BindableFloat TargetSize { get; } = new BindableFloat();

        public float MinSize { get; init; } = 50;

        public float MaxSize { get; init; } = 800;

        public HandleDirection Direction { get; init; }

        /// <summary>
        /// When <see langword="true"/>, dragging in the positive axis direction decreases the target size.
        /// </summary>
        public bool InvertDelta { get; init; }

        private const float handle_thickness = 6;

        private float dragStartSize;
        private float dragStartPosition;
        private bool dragging;

        public DesignResizeHandle()
        {
            AlwaysPresent = true;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            InternalChild = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.Transparent,
            };

            switch (Direction)
            {
                case HandleDirection.Horizontal:
                    RelativeSizeAxes = Axes.X;
                    Height = handle_thickness;
                    break;

                case HandleDirection.Vertical:
                    RelativeSizeAxes = Axes.Y;
                    Width = handle_thickness;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            dragging = true;
            dragStartSize = TargetSize.Value;
            dragStartPosition = Direction == HandleDirection.Horizontal ? e.MouseDownPosition.Y : e.MouseDownPosition.X;
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (!dragging)
                return;

            dragging = false;
            base.OnMouseUp(e);
        }

        protected override bool OnDragStart(DragStartEvent e) => dragging;

        protected override void OnDrag(DragEvent e)
        {
            float currentPosition = Direction == HandleDirection.Horizontal ? e.MousePosition.Y : e.MousePosition.X;
            float delta = currentPosition - dragStartPosition;

            if (InvertDelta)
                delta = -delta;

            TargetSize.Value = Math.Clamp(dragStartSize + delta, MinSize, MaxSize);
        }
    }
}
