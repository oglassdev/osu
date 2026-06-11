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
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Edit;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Design;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Drawables;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class CommandToolbar : EditorToolboxGroup
    {
        public CommandToolbar()
            : base(@"properties", expandedByDefault: false)
        {
            Spacing = new Vector2(0, 6);
        }

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        private EditorRadioButtonCollection commandTypeButtons = null!;
        private readonly Dictionary<StoryboardCommandType, RadioButton> commandButtonsByType = new Dictionary<StoryboardCommandType, RadioButton>();
        private GridContainer originGrid = null!;
        private FormSliderBar<float> widthSlider = null!;
        private FormSliderBar<float> heightSlider = null!;
        private FormSliderBar<float> rotationSlider = null!;
        private bool updatingTransformSliders;

        [BackgroundDependencyLoader]
        private void load()
        {
            commandTypeButtons = new EditorRadioButtonCollection { RelativeSizeAxes = Axes.X };

            commandTypeButtons.Items = StoryboardCommandCatalog.All
                .Select(descriptor => createCommandButton(descriptor))
                .ToArray();

            const float origin_button_size = 28;

            originGrid = new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, origin_button_size),
                    new Dimension(GridSizeMode.Absolute, origin_button_size),
                    new Dimension(GridSizeMode.Absolute, origin_button_size),
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, origin_button_size),
                    new Dimension(GridSizeMode.Absolute, origin_button_size),
                    new Dimension(GridSizeMode.Absolute, origin_button_size),
                },
            };

            var originButtons = new Anchor[]
            {
                Anchor.TopLeft, Anchor.CentreLeft, Anchor.BottomLeft,
                Anchor.TopCentre, Anchor.Centre, Anchor.BottomCentre,
                Anchor.TopRight, Anchor.CentreRight, Anchor.BottomRight,
            };

            originGrid.Content = new[]
            {
                createOriginRow(originButtons, 0),
                createOriginRow(originButtons, 3),
                createOriginRow(originButtons, 6),
            };

            widthSlider = createTransformSlider(@"Width", 1, 2000, applyWidth);
            heightSlider = createTransformSlider(@"Height", 1, 2000, applyHeight);
            rotationSlider = createTransformSlider(@"Rotation", -180, 180, applyRotation);

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
            {
                commandTypeButtons,
                widthSlider,
                heightSlider,
                rotationSlider,
                new OsuSpriteText { Text = @"Origin" },
                originGrid,
                new FormCheckBox
                {
                    Caption = @"Tween command",
                    Current = { BindTarget = state.Tweening },
                },
                new FormEnumDropdown<Easing>
                {
                    Caption = @"Easing",
                    Current = { BindTarget = state.Easing },
                },
                new FormSliderBar<double>
                {
                    Caption = @"Duration",
                    Current = state.TweenDuration,
                    KeyboardStep = 50,
                    LabelFormat = value => $@"{value:0} ms",
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        new RoundedButton
                        {
                            Text = @"+",
                            Width = 40,
                            Action = addKeyframe,
                        },
                        new RoundedButton
                        {
                            Text = @"-",
                            Width = 40,
                            Action = removeKeyframe,
                        },
                        new RoundedButton
                        {
                            Text = @"Previous",
                            Width = 75,
                            Action = () => seekAdjacentKeyframe(-1),
                        },
                        new RoundedButton
                        {
                            Text = @"Next",
                            Width = 60,
                            Action = () => seekAdjacentKeyframe(1),
                        },
                    },
                },
            },
            };

            state.SelectedCommandType.BindValueChanged(type =>
            {
                if (commandButtonsByType.TryGetValue(type.NewValue, out var button))
                    button.Select();
            }, true);

            state.SelectedSprite.BindValueChanged(_ => updateTransformSliders(), true);
            state.SelectedCommandType.BindValueChanged(_ => updateTransformSlidersFromDrawable());
            beatmap.TransactionEnded += updateTransformSliders;
        }

        protected override void Dispose(bool isDisposing)
        {
            beatmap.TransactionEnded -= updateTransformSliders;
            base.Dispose(isDisposing);
        }

        private FormSliderBar<float> createTransformSlider(string caption, float min, float max, Action<float> apply)
        {
            var slider = new FormSliderBar<float>
            {
                Caption = caption,
                Current = new BindableFloat(min) { MinValue = min, MaxValue = max },
                KeyboardStep = 1,
                LabelFormat = value => $@"{value:0}",
            };

            slider.Current.ValueChanged += value =>
            {
                if (updatingTransformSliders)
                    return;

                apply(value.NewValue);
            };

            return slider;
        }

        private void updateTransformSliders()
        {
            bool hasSelection = state.SelectedSprite.Value != null;
            widthSlider.Alpha = hasSelection ? 1 : 0.4f;
            heightSlider.Alpha = hasSelection ? 1 : 0.4f;
            rotationSlider.Alpha = hasSelection ? 1 : 0.4f;
            updateTransformSlidersFromDrawable();
        }

        private void updateTransformSlidersFromDrawable()
        {
            var drawable = state.Canvas?.FindDrawableSprite(state.SelectedSprite.Value);

            if (drawable == null)
                return;

            updatingTransformSliders = true;

            var size = DesignStoryboardOperations.GetSpriteDisplaySize(drawable);
            widthSlider.Current.Value = size.X;
            heightSlider.Current.Value = size.Y;
            rotationSlider.Current.Value = drawable.Rotation;

            updatingTransformSliders = false;
        }

        private void applyWidth(float width)
        {
            applyTransform(drawable =>
            {
                DesignStoryboardOperations.SetSpriteDisplayWidth(drawable, width);
            });
        }

        private void applyHeight(float height)
        {
            applyTransform(drawable =>
            {
                DesignStoryboardOperations.SetSpriteDisplayHeight(drawable, height);
            });
        }

        private void applyRotation(float rotation)
        {
            applyTransform(drawable => drawable.Rotation = rotation);
        }

        private void applyTransform(Action<DrawableStoryboardSprite> apply)
        {
            var sprite = state.SelectedSprite.Value;
            var drawable = state.Canvas?.FindDrawableSprite(sprite);

            if (sprite == null || drawable == null)
                return;

            apply(drawable);
            DesignStoryboardOperations.ApplySpriteTransform(beatmap, sprite, drawable, clock.CurrentTimeAccurate);
        }

        private RadioButton createCommandButton(StoryboardCommandDescriptor descriptor)
        {
            var button = new RadioButton(
                descriptor.DisplayName,
                () => state.SelectedCommandType.Value = descriptor.Type,
                () => new SpriteIcon { Icon = descriptor.Icon });

            commandButtonsByType[descriptor.Type] = button;

            if (state.SelectedCommandType.Value == descriptor.Type)
                button.Select();

            return button;
        }

        private Drawable[] createOriginRow(Anchor[] anchors, int startIndex)
        {
            return new[]
            {
                createOriginButton(anchors[startIndex]),
                createOriginButton(anchors[startIndex + 1]),
                createOriginButton(anchors[startIndex + 2]),
            };
        }

        private Drawable createOriginButton(Anchor origin)
        {
            return new RoundedButton
            {
                Size = new Vector2(28),
                Text = getOriginLabel(origin),
                Action = () =>
                {
                    if (state.SelectedSprite.Value != null)
                        DesignStoryboardOperations.SetSpriteOrigin(beatmap, state.SelectedSprite.Value, origin);
                },
            };
        }

        private static string getOriginLabel(Anchor anchor) => anchor switch
        {
            Anchor.TopLeft => @"TL",
            Anchor.CentreLeft => @"CL",
            Anchor.BottomLeft => @"BL",
            Anchor.TopCentre => @"TC",
            Anchor.Centre => @"C",
            Anchor.BottomCentre => @"BC",
            Anchor.TopRight => @"TR",
            Anchor.CentreRight => @"CR",
            Anchor.BottomRight => @"BR",
            _ => @"?",
        };

        private void addKeyframe()
        {
            if (state.SelectedSprite.Value == null)
                return;

            DesignStoryboardOperations.AddKeyframe(
                beatmap,
                state.SelectedSprite.Value,
                state.SelectedCommandType.Value,
                clock.CurrentTimeAccurate,
                state.Canvas?.FindDrawableSprite(state.SelectedSprite.Value),
                state.Easing.Value,
                state.Tweening.Value ? state.TweenDuration.Value : 0);
        }

        private void removeKeyframe()
        {
            if (state.SelectedSprite.Value == null)
                return;

            DesignStoryboardOperations.RemoveKeyframe(
                beatmap,
                state.SelectedSprite.Value,
                state.SelectedCommandType.Value,
                clock.CurrentTimeAccurate);
        }

        private void seekAdjacentKeyframe(int direction)
        {
            if (state.SelectedSprite.Value == null)
                return;

            double? time = DesignStoryboardOperations.FindAdjacentKeyframeTime(
                state.SelectedSprite.Value,
                state.SelectedCommandType.Value,
                clock.CurrentTimeAccurate,
                direction);

            if (time != null)
                clock.Seek(time.Value);
        }
    }
}
