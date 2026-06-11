// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Storyboards.Drawables;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Screens.Edit.Design
{
    internal partial class DesignStoryboardSelectionScaleHandler : SelectionScaleHandler
    {
        public event Action<Axes>? PerformFlipFromScaleHandles;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        private OriginalSpriteState? originalState;
        private Vector2? defaultOrigin;

        private bool isFlippedX;
        private bool isFlippedY;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            state.SelectedSprite.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            CanScaleX.Value = state.SelectedSprite.Value != null;
            CanScaleY.Value = state.SelectedSprite.Value != null;
            CanScaleDiagonally.Value = state.SelectedSprite.Value != null;
        }

        public override void Begin()
        {
            if (originalState != null)
                throw new InvalidOperationException($"Cannot {nameof(Begin)} a scale operation while another is in progress!");

            var drawable = getDrawableSprite();

            if (drawable == null)
                return;

            beatmap.BeginChange();

            originalState = new OriginalSpriteState(drawable);
            OriginalSurroundingQuad = ToLocalSpace(drawable.ScreenSpaceDrawQuad);
            defaultOrigin = ToLocalSpace(GeometryUtils.MinimumEnclosingCircle(drawable.ScreenSpaceDrawQuad.GetVertices().ToArray()).Item1);

            isFlippedX = false;
            isFlippedY = false;

            base.Begin();
        }

        public override void Update(Vector2 scale, Vector2? origin = null, Axes adjustAxis = Axes.Both, float axisRotation = 0)
        {
            if (originalState == null)
                throw new InvalidOperationException($"Cannot {nameof(Update)} a scale operation without calling {nameof(Begin)} first!");

            var drawable = getDrawableSprite();

            if (drawable == null)
                return;

            Debug.Assert(defaultOrigin != null && OriginalSurroundingQuad != null);

            var actualOrigin = ToScreenSpace(origin ?? defaultOrigin.Value);

            if (OriginalSurroundingQuad.Value.Width == 0 || OriginalSurroundingQuad.Value.Height == 0)
                return;

            if (adjustAxis == Axes.Both)
                scale = new Vector2((scale.X + scale.Y) * 0.5f);

            bool flippedX = scale.X < 0;
            bool flippedY = scale.Y < 0;
            Axes toFlip = Axes.None;

            if (flippedX != isFlippedX)
            {
                isFlippedX = flippedX;
                toFlip |= Axes.X;
            }

            if (flippedY != isFlippedY)
            {
                isFlippedY = flippedY;
                toFlip |= Axes.Y;
            }

            if (toFlip != Axes.None)
            {
                PerformFlipFromScaleHandles?.Invoke(toFlip);
                return;
            }

            var currentScale = scale;

            if (Precision.AlmostEquals(MathF.Abs(drawable.Rotation) % 180, 90))
                currentScale = new Vector2(scale.Y, scale.X);

            updateDrawablePosition(drawable, GeometryUtils.GetScaledPosition(currentScale, actualOrigin, originalState.Value.ScreenSpaceOriginPosition));

            switch (adjustAxis)
            {
                case Axes.X:
                    drawable.VectorScale = new Vector2(
                        MathF.Abs(originalState.Value.VectorScale.X * currentScale.X),
                        originalState.Value.VectorScale.Y);
                    break;

                case Axes.Y:
                    drawable.VectorScale = new Vector2(
                        originalState.Value.VectorScale.X,
                        MathF.Abs(originalState.Value.VectorScale.Y * currentScale.Y));
                    break;

                case Axes.Both:
                    drawable.VectorScale = originalState.Value.VectorScale * currentScale;
                    break;
            }
        }

        public override void Commit()
        {
            if (originalState == null)
                return;

            var drawable = getDrawableSprite();
            var sprite = state.SelectedSprite.Value;

            if (drawable != null && sprite != null)
                DesignStoryboardOperations.CommitDrawableTransform(sprite, drawable, clock.CurrentTimeAccurate);

            beatmap.EndChange();

            originalState = null;
            defaultOrigin = null;

            base.Commit();
        }

        private DrawableStoryboardSprite? getDrawableSprite()
            => state.Canvas?.FindDrawableSprite(state.SelectedSprite.Value);

        private static void updateDrawablePosition(DrawableStoryboardSprite drawable, Vector2 screenSpacePosition)
        {
            drawable.Position = drawable.Parent!.ToLocalSpace(screenSpacePosition) - drawable.AnchorPosition;
        }

        private readonly struct OriginalSpriteState
        {
            public Vector2 VectorScale { get; }
            public Vector2 ScreenSpaceOriginPosition { get; }

            public OriginalSpriteState(DrawableStoryboardSprite drawable)
            {
                VectorScale = drawable.VectorScale;
                ScreenSpaceOriginPosition = drawable.ToScreenSpace(drawable.OriginPosition);
            }
        }
    }
}
