// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Storyboards.Drawables;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Screens.Edit.Design
{
    internal partial class DesignStoryboardSelectionRotationHandler : SelectionRotationHandler
    {
        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        private float? originalRotation;
        private Vector2? originalScreenSpaceOrigin;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            state.SelectedSprite.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            CanRotateAroundSelectionOrigin.Value = state.SelectedSprite.Value != null;
        }

        public override void Begin()
        {
            if (originalRotation != null)
                throw new InvalidOperationException($"Cannot {nameof(Begin)} a rotate operation while another is in progress!");

            var drawable = getDrawableSprite();

            if (drawable == null)
                return;

            beatmap.BeginChange();

            originalRotation = drawable.Rotation;
            originalScreenSpaceOrigin = drawable.ToScreenSpace(drawable.OriginPosition);
            DefaultOrigin = ToLocalSpace(drawable.ScreenSpaceDrawQuad).Centre;

            base.Begin();
        }

        public override void Update(float rotation, Vector2? origin = null)
        {
            if (originalRotation == null)
                throw new InvalidOperationException($"Cannot {nameof(Update)} a rotate operation without calling {nameof(Begin)} first!");

            var drawable = getDrawableSprite();

            if (drawable == null)
                return;

            Debug.Assert(originalScreenSpaceOrigin != null && DefaultOrigin != null);

            drawable.Rotation = originalRotation.Value + rotation;

            var actualOrigin = origin ?? DefaultOrigin.Value;
            var rotatedPosition = GeometryUtils.RotatePointAroundOrigin(originalScreenSpaceOrigin.Value, ToScreenSpace(actualOrigin), rotation);
            updateDrawablePosition(drawable, rotatedPosition);
        }

        public override void Commit()
        {
            if (originalRotation == null)
                return;

            var drawable = getDrawableSprite();
            var sprite = state.SelectedSprite.Value;

            if (drawable != null && sprite != null)
                DesignStoryboardOperations.applySpriteTransform(sprite, drawable, clock.CurrentTimeAccurate);

            beatmap.EndChange();

            originalRotation = null;
            originalScreenSpaceOrigin = null;
            DefaultOrigin = null;

            base.Commit();
        }

        private DrawableStoryboardSprite? getDrawableSprite()
            => state.Canvas?.FindDrawableSprite(state.SelectedSprite.Value);

        private static void updateDrawablePosition(DrawableStoryboardSprite drawable, Vector2 screenSpacePosition)
        {
            drawable.Position = drawable.Parent!.ToLocalSpace(screenSpacePosition) - drawable.AnchorPosition;
        }
    }
}
