// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Design;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class DesignReadingsPanel : EditorToolboxGroup
    {
        public DesignReadingsPanel()
            : base(@"inspector", expandedByDefault: false)
        {
            Spacing = new Vector2(0, 6);
        }

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        private OsuSpriteText timeText = null!;
        private OsuSpriteText mouseText = null!;
        private OsuSpriteText selectionText = null!;
        private OsuSpriteText loadText = null!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
                {
                    selectionText = new OsuSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                    },
                    timeText = new OsuSpriteText(),
                    mouseText = new OsuSpriteText(),
                    loadText = new OsuSpriteText(),
                },
            };

            state.CanvasMousePosition.BindValueChanged(_ => updateMouseText(), true);
            state.SelectedSprite.BindValueChanged(selection => updateSelection(selection.NewValue), true);
            beatmap.TransactionEnded += updateLoadEstimate;
            beatmap.SaveStateTriggered += updateLoadEstimate;
            updateLoadEstimate();
        }

        protected override void Update()
        {
            base.Update();
            timeText.Text = $@"Time: {clock.CurrentTimeAccurate.ToString(@"F0", CultureInfo.InvariantCulture)} ms";
        }

        private void updateMouseText()
        {
            if (state.CanvasMousePosition.Value is Vector2 position)
            {
                mouseText.Text = $@"Mouse: ({position.X.ToString(@"F0", CultureInfo.InvariantCulture)}, {position.Y.ToString(@"F0", CultureInfo.InvariantCulture)})";
            }
            else
                mouseText.Text = @"Mouse: (-, -)";
        }

        private void updateSelection(StoryboardSprite? sprite)
        {
            selectionText.Text = sprite == null
                ? @"No object selected"
                : $@"{sprite.Path}
Origin: {sprite.Origin}
Loops: {sprite.LoopingGroups.Count}
Triggers: {sprite.TriggerGroups.Count}";
        }

        private void updateLoadEstimate()
        {
            int elements = beatmap.Storyboard.Layers.Sum(layer => layer.Elements.Count);
            int commands = beatmap.Storyboard.Layers.SelectMany(layer => layer.Elements)
                                  .OfType<StoryboardSprite>()
                                  .Sum(sprite => sprite.Commands.AllCommands.Count()
                                               + sprite.LoopingGroups.Sum(loop => loop.AllCommands.Count())
                                               + sprite.TriggerGroups.Sum(trigger => trigger.AllCommands.Count()));

            loadText.Text = $@"Storyboard load: {elements} elements / {commands} commands";
        }

        protected override void Dispose(bool isDisposing)
        {
            beatmap.TransactionEnded -= updateLoadEstimate;
            beatmap.SaveStateTriggered -= updateLoadEstimate;
            base.Dispose(isDisposing);
        }
    }
}
