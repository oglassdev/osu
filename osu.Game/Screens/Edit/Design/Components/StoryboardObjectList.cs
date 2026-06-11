// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit.Design;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class StoryboardObjectList : EditorToolboxGroup
    {
        public StoryboardObjectList()
            : base(@"objects", expandedByDefault: false)
        {
            Spacing = new Vector2(0, 6);
        }

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        private FillFlowContainer layerFlows = null!;
        private RoundedButton deleteButton = null!;
        private OsuDropdown<string> layerDropdown = null!;
        private FormEnumDropdown<StoryboardElementSource> sourceDropdown = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            layerFlows = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
            };

            layerDropdown = new OsuDropdown<string>
            {
                RelativeSizeAxes = Axes.X,
                Items = DesignStoryboardOperations.EDITABLE_LAYERS,
            };

            layerDropdown.Current.BindValueChanged(selection =>
            {
                if (state.SelectedSprite.Value != null && selection.NewValue != null)
                    DesignStoryboardOperations.ChangeSpriteLayer(beatmap, state.SelectedSprite.Value, selection.NewValue);
            });

            deleteButton = new RoundedButton
            {
                Text = @"Delete selected",
                RelativeSizeAxes = Axes.X,
                Action = () =>
                {
                    if (state.SelectedSprite.Value != null)
                    {
                        var sprite = state.SelectedSprite.Value;
                        DesignStoryboardOperations.DeleteSprite(beatmap, sprite);
                        state.SelectedSprite.Value = null;
                    }
                },
            };

            sourceDropdown = new FormEnumDropdown<StoryboardElementSource>
            {
                Caption = @"Source",
            };

            sourceDropdown.Current.BindValueChanged(selection =>
            {
                if (state.SelectedSprite.Value != null && selection.NewValue != state.SelectedSprite.Value.Source)
                    state.SelectedSprite.Value = DesignStoryboardOperations.ChangeSpriteSource(beatmap, state.SelectedSprite.Value, selection.NewValue);
            });

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
                {
                    layerFlows,
                    new OsuSpriteText { Text = @"Layer" },
                    layerDropdown,
                    sourceDropdown,
                    deleteButton,
                },
            };

            beatmap.TransactionEnded += refreshList;
            beatmap.SaveStateTriggered += refreshList;
            state.SelectedSprite.BindValueChanged(selection => updateLayerDropdown(selection.NewValue), true);

            refreshList();
        }

        private void refreshList()
        {
            layerFlows.Clear();

            foreach (string layerName in DesignStoryboardOperations.EDITABLE_LAYERS)
            {
                var layer = beatmap.Storyboard.GetLayer(layerName);
                var sprites = layer.Elements.OfType<StoryboardSprite>().ToArray();
                var samples = layer.Elements.OfType<StoryboardSampleInfo>().ToArray();

                if (sprites.Length == 0 && samples.Length == 0)
                    continue;

                layerFlows.Add(new OsuSpriteText { Text = layerName, Font = OsuFont.Default.With(weight: @"bold") });

                foreach (var sprite in sprites)
                    layerFlows.Add(new SpriteListItem(sprite, selectSprite));

                foreach (var sample in samples)
                    layerFlows.Add(new SampleListItem(sample, deleteSample));
            }
        }

        private void selectSprite(StoryboardSprite sprite)
        {
            state.SelectedSprite.Value = sprite;
            clock.Seek(sprite.StartTime);
        }

        private void updateLayerDropdown(StoryboardSprite? sprite)
        {
            deleteButton.Enabled.Value = sprite != null;

            if (sprite != null)
            {
                layerDropdown.Current.Value = DesignStoryboardOperations.FindLayerName(beatmap.Storyboard, sprite) ?? DesignStoryboardOperations.EDITABLE_LAYERS[0];
                sourceDropdown.Current.Value = sprite.Source;
            }
        }

        private void deleteSample(StoryboardSampleInfo sample) => DesignStoryboardOperations.DeleteElement(beatmap, sample);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            beatmap.TransactionEnded -= refreshList;
            beatmap.SaveStateTriggered -= refreshList;
        }

        private partial class SpriteListItem : CompositeDrawable
        {
            private readonly StoryboardSprite sprite;
            private readonly Action<StoryboardSprite> onSelected;

            public SpriteListItem(StoryboardSprite sprite, Action<StoryboardSprite> onSelected)
            {
                this.sprite = sprite;
                this.onSelected = onSelected;

                RelativeSizeAxes = Axes.X;
                Height = 25;
            }

            [BackgroundDependencyLoader]
            private void load(DesignStoryboardState state)
            {
                InternalChild = new TruncatingSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = sprite.Path,
                    RelativeSizeAxes = Axes.X,
                    Width = 0.9f,
                };

                state.SelectedSprite.BindValueChanged(selection => Alpha = selection.NewValue == sprite ? 1 : 0.6f, true);
            }

            protected override bool OnClick(ClickEvent e)
            {
                onSelected(sprite);
                return true;
            }
        }

        private partial class SampleListItem : CompositeDrawable
        {
            private readonly StoryboardSampleInfo sample;
            private readonly Action<StoryboardSampleInfo> onDelete;

            public SampleListItem(StoryboardSampleInfo sample, Action<StoryboardSampleInfo> onDelete)
            {
                this.sample = sample;
                this.onDelete = onDelete;

                RelativeSizeAxes = Axes.X;
                Height = 30;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
                {
                    new TruncatingSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.7f,
                        Text = $@"{sample.Path} ({sample.Volume}%)",
                    },
                    new RoundedButton
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Width = 55,
                        Height = 25,
                        Text = @"Delete",
                        Action = () => onDelete(sample),
                    },
                };
            }
        }
    }
}
