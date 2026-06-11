// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Design.Components;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Drawables;
using osuTK;

namespace osu.Game.Screens.Edit.Design
{
    public partial class DesignStoryboardCanvas : CompositeDrawable
    {
        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        private Container playfieldContent = null!;
        private Container storyboardHost = null!;
        private DrawableStoryboard? drawableStoryboard;
        private DesignStoryboardSelectionOverlay? selectionOverlay;
        private OsuSpriteText placementStatus = null!;
        private int loadGeneration;
        private int lastStructureSignature;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = playfieldContent = new Container
            {
                Name = @"Playfield content",
                RelativeSizeAxes = Axes.Y,
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new DesignBeatmapPreview(),
                        storyboardHost = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        selectionOverlay = new DesignStoryboardSelectionOverlay(),
                        placementStatus = new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Y = 10,
                            Alpha = 0,
                        },
                    },
                },
            };

            beatmap.TransactionEnded += onStoryboardChanged;
            beatmap.SaveStateTriggered += onStoryboardChanged;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            state.Canvas = this;

            state.ShowBackground.BindValueChanged(_ => updateLayerVisibility(), true);
            state.ShowFail.BindValueChanged(_ => updateLayerVisibility(), true);
            state.ShowPass.BindValueChanged(_ => updateLayerVisibility(), true);
            state.ShowForeground.BindValueChanged(_ => updateLayerVisibility(), true);
            state.PreviewPassing.BindValueChanged(_ => updateLayerVisibility(), true);
            state.PlacementPath.BindValueChanged(path =>
            {
                placementStatus.Text = path.NewValue == null ? string.Empty : $@"Click the canvas to place {path.NewValue}";
                placementStatus.FadeTo(path.NewValue == null ? 0 : 1, 150);
            }, true);

            lastStructureSignature = computeStructureSignature();
            rebuildStoryboard();
        }

        protected override void Update()
        {
            base.Update();

            playfieldContent.Anchor = Anchor.Centre;
            playfieldContent.Origin = Anchor.Centre;
            playfieldContent.Width = Math.Max(1024, DrawWidth);

            state.CanvasMousePosition.Value = getMousePositionOnCanvas();
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (state.PlacementPath.Value is string path)
            {
                Vector2 position = getStoryboardPosition(e.ScreenSpaceMousePosition);
                state.SelectedSprite.Value = DesignStoryboardOperations.PlaceSprite(
                    beatmap,
                    path,
                    clock.CurrentTimeAccurate,
                    position,
                    textureSize: state.PlacementTextureSize.Value);
                state.PlacementPath.Value = null;
                state.PlacementTextureSize.Value = null;
                return true;
            }

            if (drawableStoryboard == null)
            {
                state.SelectedSprite.Value = null;
                return true;
            }

            var sprites = getDrawableSprites()
                .Where(s => s.IsPresent)
                .Reverse();

            foreach (var sprite in sprites)
            {
                if (sprite.ScreenSpaceDrawQuad.Contains(e.ScreenSpaceMousePosition))
                {
                    state.SelectedSprite.Value = sprite.Sprite;
                    return true;
                }
            }

            state.SelectedSprite.Value = null;
            return true;
        }

        public DrawableStoryboardSprite? FindDrawableSprite(StoryboardSprite? sprite)
        {
            if (drawableStoryboard == null || sprite == null)
                return null;

            return getDrawableSprites().SingleOrDefault(s => s.Sprite == sprite);
        }

        private IEnumerable<DrawableStoryboardSprite> getDrawableSprites()
        {
            if (drawableStoryboard == null)
                yield break;

            foreach (var layer in drawableStoryboard.Children)
            {
                foreach (var child in layer.DrawableElements)
                {
                    if (child is DrawableStoryboardSprite sprite)
                        yield return sprite;
                }
            }
        }

        private void onStoryboardChanged()
        {
            int signature = computeStructureSignature();

            if (signature == lastStructureSignature)
                return;

            lastStructureSignature = signature;
            rebuildStoryboard();
        }

        private int computeStructureSignature()
        {
            int signature = 0;

            foreach (var layer in beatmap.Storyboard.Layers)
            {
                signature = HashCode.Combine(signature, layer.Name);

                foreach (var element in layer.Elements)
                {
                    signature = HashCode.Combine(signature, element.GetHashCode());

                    if (element is StoryboardSprite sprite)
                        signature = HashCode.Combine(signature, sprite.Path);
                }
            }

            return signature;
        }

        private void rebuildStoryboard()
        {
            int generation = ++loadGeneration;

            var storyboard = new DrawableStoryboard(beatmap.Storyboard)
            {
                Clock = clock,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };

            LoadComponentAsync(storyboard, loaded =>
            {
                if (generation != loadGeneration)
                {
                    loaded.Expire();
                    return;
                }

                if (drawableStoryboard != null)
                    storyboardHost.Remove(drawableStoryboard, false);

                drawableStoryboard = loaded;
                storyboardHost.Add(loaded);
                updateLayerVisibility();
            });
        }

        private void updateLayerVisibility()
        {
            if (drawableStoryboard == null)
                return;

            foreach (var layer in drawableStoryboard.Children)
            {
                layer.Enabled = layer.Name switch
                {
                    @"Background" => state.ShowBackground.Value,
                    @"Fail" => state.ShowFail.Value && !state.PreviewPassing.Value,
                    @"Pass" => state.ShowPass.Value && state.PreviewPassing.Value,
                    @"Foreground" => state.ShowForeground.Value,
                    _ => layer.Enabled,
                };
            }
        }

        private Vector2? getMousePositionOnCanvas()
        {
            if (drawableStoryboard == null || !drawableStoryboard.IsLoaded)
                return null;

            var inputManager = GetContainingInputManager();

            if (inputManager == null)
                return null;

            var mousePosition = ToLocalSpace(inputManager.CurrentState.Mouse.Position);

            if (!DrawRectangle.Contains(mousePosition))
                return null;

            return drawableStoryboard.ToLocalSpace(ToScreenSpace(mousePosition));
        }

        private Vector2 getStoryboardPosition(Vector2 screenSpacePosition)
        {
            if (drawableStoryboard != null && drawableStoryboard.IsLoaded)
            {
                Vector2 position = drawableStoryboard.ToLocalSpace(screenSpacePosition);
                return new Vector2(
                    Math.Clamp(position.X, 0, drawableStoryboard.Width),
                    Math.Clamp(position.Y, 0, drawableStoryboard.Height));
            }

            // Storyboard not yet loaded; map through the host using the same aspect as DrawableStoryboard.
            Vector2 hostPosition = storyboardHost.ToLocalSpace(screenSpacePosition);
            Vector2 size = getStoryboardSize();
            Vector2 topLeft = (storyboardHost.DrawSize - size) / 2f;
            Vector2 storyboardPosition = hostPosition - topLeft;

            return new Vector2(
                Math.Clamp(storyboardPosition.X, 0, size.X),
                Math.Clamp(storyboardPosition.Y, 0, size.Y));
        }

        private Vector2 getStoryboardSize()
        {
            const float height = 480;
            float width = height * (beatmap.WidescreenStoryboard ? 16 / 9f : 4 / 3f);
            return new Vector2(width, height);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (state.Canvas == this)
                state.Canvas = null;

            beatmap.TransactionEnded -= onStoryboardChanged;
            beatmap.SaveStateTriggered -= onStoryboardChanged;
        }

    }
}
