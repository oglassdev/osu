// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Storyboards;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class StoryboardOverviewTimeline : CompositeDrawable
    {
        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        private Container tracks = null!;
        private Color4[] layerColours = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            Height = 0.82f;

            layerColours = new[] { colours.Blue1, colours.Pink1, colours.Green1, colours.Purple1 };

            InternalChild = tracks = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };

            beatmap.TransactionEnded += refresh;
            beatmap.SaveStateTriggered += refresh;
            refresh();
        }

        private void refresh()
        {
            tracks.Clear();

            for (int layerIndex = 0; layerIndex < DesignStoryboardOperations.EDITABLE_LAYERS.Length; layerIndex++)
            {
                string layerName = DesignStoryboardOperations.EDITABLE_LAYERS[layerIndex];

                foreach (var sprite in beatmap.Storyboard.GetLayer(layerName).Elements.OfType<StoryboardSprite>())
                {
                    double duration = Math.Max(20, sprite.EndTimeForDisplay - sprite.StartTime);

                    tracks.Add(new SpriteSpan(sprite, layerColours[layerIndex], selectSprite)
                    {
                        RelativePositionAxes = Axes.X | Axes.Y,
                        RelativeSizeAxes = Axes.X | Axes.Y,
                        X = (float)sprite.StartTime,
                        Y = layerIndex / 4f,
                        Width = (float)duration,
                        Height = 0.21f,
                    });
                }
            }
        }

        private void selectSprite(StoryboardSprite sprite)
        {
            state.SelectedSprite.Value = sprite;
            clock.Seek(sprite.StartTime);
        }

        protected override void Dispose(bool isDisposing)
        {
            beatmap.TransactionEnded -= refresh;
            beatmap.SaveStateTriggered -= refresh;
            base.Dispose(isDisposing);
        }

        private partial class SpriteSpan : CompositeDrawable
        {
            private readonly StoryboardSprite sprite;
            private readonly Action<StoryboardSprite> action;
            private readonly Color4 colour;

            public SpriteSpan(StoryboardSprite sprite, Color4 colour, Action<StoryboardSprite> action)
            {
                this.sprite = sprite;
                this.colour = colour;
                this.action = action;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Masking = true;
                CornerRadius = 2;
                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colour,
                    Alpha = 0.8f,
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                action(sprite);
                return true;
            }
        }
    }
}
