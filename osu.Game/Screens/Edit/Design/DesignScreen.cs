// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Screens.Edit.Design.Components;
using osu.Game.Storyboards;

namespace osu.Game.Screens.Edit.Design
{
    public partial class DesignScreen : EditorScreenWithTimeline
    {
        [Cached]
        public DesignStoryboardState StoryboardState { get; } = new DesignStoryboardState();

        public DesignScreen()
            : base(EditorScreenMode.Design)
        {
        }

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        private StoryboardSprite? clipboardSprite;
        private string? clipboardLayer;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(StoryboardState);
            return dependencies;
        }

        protected override Drawable CreateMainContent() => new EditorSkinProvidingContainer(EditorBeatmap)
        {
            RelativeSizeAxes = Axes.Both,
            Child = new DesignEditor(),
        };

        protected override Drawable CreateTimelineContent() => new StoryboardOverviewTimeline();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            StoryboardState.SelectedSprite.BindValueChanged(_ =>
            {
                CanCut.Value = CanCopy.Value = StoryboardState.SelectedSprite.Value != null;
                CanPaste.Value = clipboardSprite != null;
            }, true);
        }

        public override void Cut()
        {
            if (!CanCut.Value || StoryboardState.SelectedSprite.Value == null)
                return;

            clipboardSprite = StoryboardState.SelectedSprite.Value;
            clipboardLayer = DesignStoryboardOperations.FindLayerName(EditorBeatmap.Storyboard, clipboardSprite);
            DesignStoryboardOperations.DeleteSprite(EditorBeatmap, StoryboardState.SelectedSprite.Value);
            StoryboardState.SelectedSprite.Value = null;
            CanPaste.Value = true;
        }

        public override void Copy()
        {
            if (!CanCopy.Value)
                return;

            clipboardSprite = StoryboardState.SelectedSprite.Value;
            clipboardLayer = clipboardSprite == null ? null : DesignStoryboardOperations.FindLayerName(EditorBeatmap.Storyboard, clipboardSprite);
            CanPaste.Value = clipboardSprite != null;
        }

        public override void Paste()
        {
            if (!CanPaste.Value || clipboardSprite == null)
                return;

            double offset = clock.CurrentTimeAccurate - clipboardSprite.StartTime;
            StoryboardState.SelectedSprite.Value = DesignStoryboardOperations.CloneSprite(EditorBeatmap, clipboardSprite, offset, targetLayer: clipboardLayer);
        }
    }
}
