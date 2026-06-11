// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Storyboards;
using osu.Game.Rulesets.Edit;
using osu.Game.Utils;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class StoryboardElementCreationPanel : CompositeDrawable
    {
        public StoryboardElementCreationPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

        private readonly Bindable<FileInfo?> animationFile = new Bindable<FileInfo?>();
        private readonly Bindable<FileInfo?> sampleFile = new Bindable<FileInfo?>();
        private readonly BindableInt frameCount = new BindableInt(1) { MinValue = 1, MaxValue = 1000 };
        private readonly BindableDouble frameDelay = new BindableDouble(100) { MinValue = 1, MaxValue = 10000 };
        private readonly Bindable<AnimationLoopType> loopType = new Bindable<AnimationLoopType>(AnimationLoopType.LoopForever);
        private readonly BindableInt sampleVolume = new BindableInt(100) { MinValue = 0, MaxValue = 100 };
        private readonly BindableInt loopRepeats = new BindableInt(1) { MinValue = 0, MaxValue = 1000 };
        private readonly BindableDouble loopDuration = new BindableDouble(1000) { MinValue = 1, MaxValue = 60000 };
        private readonly Bindable<string> triggerName = new Bindable<string>(@"Passing");
        private readonly BindableDouble triggerDuration = new BindableDouble(10000) { MinValue = 0, MaxValue = 600000 };
        private readonly BindableInt triggerGroup = new BindableInt { MinValue = 0, MaxValue = 1000 };

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
            {
                new FormSliderBar<int>
                {
                    Caption = @"Animation frames",
                    Current = frameCount,
                    KeyboardStep = 1,
                },
                new FormSliderBar<double>
                {
                    Caption = @"Frame delay",
                    Current = frameDelay,
                    KeyboardStep = 10,
                    LabelFormat = value => $@"{value:0} ms",
                },
                new FormEnumDropdown<AnimationLoopType>
                {
                    Caption = @"Animation loop",
                    Current = loopType,
                },
                new FormFileSelector(SupportedExtensions.IMAGE_EXTENSIONS)
                {
                    Caption = @"Import animation",
                    PlaceholderText = @"Choose first frame",
                    Current = animationFile,
                },
                new FormSliderBar<int>
                {
                    Caption = @"Sample volume",
                    Current = sampleVolume,
                    KeyboardStep = 5,
                },
                new FormFileSelector(SupportedExtensions.AUDIO_EXTENSIONS)
                {
                    Caption = @"Import sample",
                    PlaceholderText = @"Choose audio file",
                    Current = sampleFile,
                },
                new FormSliderBar<int>
                {
                    Caption = @"Loop repeats",
                    Current = loopRepeats,
                    KeyboardStep = 1,
                },
                new FormSliderBar<double>
                {
                    Caption = @"Loop duration",
                    Current = loopDuration,
                    KeyboardStep = 100,
                    LabelFormat = value => $@"{value:0} ms",
                },
                new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = @"Add loop to selected",
                    Action = addLoop,
                },
                new FormTextBox
                {
                    Caption = @"Trigger name",
                    Current = triggerName,
                },
                new FormSliderBar<double>
                {
                    Caption = @"Trigger window",
                    Current = triggerDuration,
                    KeyboardStep = 1000,
                    LabelFormat = value => $@"{value:0} ms",
                },
                new FormSliderBar<int>
                {
                    Caption = @"Trigger group",
                    Current = triggerGroup,
                    KeyboardStep = 1,
                },
                new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = @"Add trigger to selected",
                    Action = addTrigger,
                },
            },
            };

            animationFile.BindValueChanged(file =>
            {
                if (file.NewValue == null)
                    return;

                import(file.NewValue);
                state.SelectedSprite.Value = DesignStoryboardOperations.PlaceAnimation(
                    beatmap,
                    file.NewValue.Name,
                    clock.CurrentTimeAccurate,
                    frameCount.Value,
                    frameDelay.Value,
                    loopType.Value);
                animationFile.Value = null;
            });

            sampleFile.BindValueChanged(file =>
            {
                if (file.NewValue == null)
                    return;

                import(file.NewValue);
                DesignStoryboardOperations.PlaceSample(beatmap, file.NewValue.Name, clock.CurrentTimeAccurate, sampleVolume.Value);
                sampleFile.Value = null;
            });
        }

        private void import(FileInfo file)
        {
            using var stream = file.OpenRead();
            beatmaps.AddFile(workingBeatmap.Value.BeatmapSetInfo, stream, file.Name);
        }

        private void addLoop()
        {
            if (state.SelectedSprite.Value != null)
                DesignStoryboardOperations.AddLoop(beatmap, state.SelectedSprite.Value, clock.CurrentTimeAccurate, loopRepeats.Value, loopDuration.Value);
        }

        private void addTrigger()
        {
            if (state.SelectedSprite.Value == null || string.IsNullOrWhiteSpace(triggerName.Value))
                return;

            DesignStoryboardOperations.AddTrigger(
                beatmap,
                state.SelectedSprite.Value,
                triggerName.Value.Trim(),
                clock.CurrentTimeAccurate,
                clock.CurrentTimeAccurate + triggerDuration.Value,
                triggerGroup.Value);
        }
    }
}
