// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class DesignStoryboardSelectionOverlay : CompositeDrawable
    {
        private const float inflate_size = 5;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        private SelectionBox selectionBox = null!;
        private DesignStoryboardSelectionScaleHandler scaleHandler = null!;
        private DesignStoryboardSelectionRotationHandler rotationHandler = null!;

        public DesignStoryboardSelectionOverlay()
        {
            AlwaysPresent = true;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(scaleHandler = new DesignStoryboardSelectionScaleHandler());
            dependencies.CacheAs(rotationHandler = new DesignStoryboardSelectionRotationHandler());
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                scaleHandler,
                rotationHandler,
                selectionBox = new SelectionBox
                {
                    OnFlip = handleFlip,
                },
            };

            scaleHandler.PerformFlipFromScaleHandles += axes => selectionBox.PerformFlipFromScaleHandles(axes);

            selectionBox.CanFlipX = true;
            selectionBox.CanFlipY = true;
            selectionBox.CanReverse = false;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            state.SelectedSprite.BindValueChanged(_ => updateVisibility(), true);
        }

        protected override void Update()
        {
            base.Update();

            var drawable = state.Canvas?.FindDrawableSprite(state.SelectedSprite.Value);

            if (drawable == null || !drawable.IsPresent)
            {
                selectionBox.Alpha = 0;
                return;
            }

            RectangleF selectionRect = ToLocalSpace(drawable.ScreenSpaceDrawQuad).AABBFloat;
            selectionRect = selectionRect.Inflate(inflate_size);

            selectionBox.Position = selectionRect.Location;
            selectionBox.Size = selectionRect.Size;
            selectionBox.Alpha = 1;
        }

        private void updateVisibility()
        {
            selectionBox.Text = state.SelectedSprite.Value == null ? string.Empty : @"1";
        }

        private bool handleFlip(Direction direction, bool flipOverOrigin)
        {
            var sprite = state.SelectedSprite.Value;
            var drawable = state.Canvas?.FindDrawableSprite(sprite);

            if (drawable == null || sprite == null)
                return false;

            switch (direction)
            {
                case Direction.Horizontal:
                    drawable.FlipH = !drawable.FlipH;
                    break;

                case Direction.Vertical:
                    drawable.FlipV = !drawable.FlipV;
                    break;
            }

            if (!scaleHandler.OperationInProgress.Value && !rotationHandler.OperationInProgress.Value)
                DesignStoryboardOperations.ApplySpriteTransform(beatmap, sprite, drawable, clock.CurrentTimeAccurate);

            return true;
        }
    }
}
