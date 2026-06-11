// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Design;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Design.Components;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneDesignScreen : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Test]
        public void TestDesignModeWorkflow()
        {
            DesignScreen? designScreen = null;
            StoryboardSprite? placedSprite = null;

            AddStep(@"switch to design mode", () => Editor.Mode.Value = EditorScreenMode.Design);
            AddUntilStep(@"design screen loaded", () => (designScreen = Editor.ChildrenOfType<DesignScreen>().SingleOrDefault())?.IsLoaded == true);

            AddStep(@"place sprite", () =>
            {
                DesignStoryboardOperations.PlaceSprite(EditorBeatmap, @"sb/test.png", EditorClock.CurrentTimeAccurate);
                placedSprite = EditorBeatmap.Storyboard.GetLayer(@"Foreground").Elements.OfType<StoryboardSprite>().Last();
                designScreen!.StoryboardState.SelectedSprite.Value = placedSprite;
            });

            AddAssert(@"sprite placed on foreground", () => placedSprite != null && DesignStoryboardOperations.FindLayerName(EditorBeatmap.Storyboard, placedSprite) == @"Foreground");
            AddAssert(@"sprite is drawable", () => placedSprite!.IsDrawable);

            AddStep(@"add move keyframe", () =>
            {
                DesignStoryboardOperations.AddKeyframe(
                    EditorBeatmap,
                    placedSprite!,
                    StoryboardCommandType.Move,
                    EditorClock.CurrentTimeAccurate,
                    designScreen!.StoryboardState.Canvas?.FindDrawableSprite(placedSprite));
            });

            AddAssert(@"move keyframe added", () => placedSprite!.Commands.X.Any() && placedSprite.Commands.Y.Any());
            AddAssert(@"canvas present", () => designScreen!.StoryboardState.Canvas != null);
            AddAssert(@"design editor fills main content", () =>
            {
                var designEditor = designScreen!.ChildrenOfType<DesignEditor>().Single();
                return designEditor.DrawWidth > 500 && designEditor.DrawHeight > 300;
            });
            AddAssert(@"design toolbox present", () => designScreen!.ChildrenOfType<DesignToolbox>().SingleOrDefault() != null);
            AddAssert(@"transformation timeline present", () => designScreen!.ChildrenOfType<TransformationTimeline>().SingleOrDefault() != null);
            AddAssert(@"storyboard overview present", () => designScreen!.ChildrenOfType<StoryboardOverviewTimeline>().SingleOrDefault() != null);
            AddAssert(@"timeline is docked below canvas", () =>
                designScreen!.StoryboardState.Canvas!.ScreenSpaceDrawQuad.BottomLeft.Y
                <= designScreen.ChildrenOfType<TransformationTimeline>().Single().ScreenSpaceDrawQuad.TopLeft.Y);

            AddStep(@"open sprites tool", () => Editor.ChildrenOfType<DesignDialogToolButton>().First().TriggerClick());
            AddUntilStep(@"sprite library is visible", () =>
            {
                var fileSelector = Editor.ChildrenOfType<SpriteLibraryPanel>().SingleOrDefault()?.ChildrenOfType<FormFileSelector>().SingleOrDefault();
                return fileSelector?.IsPresent == true && fileSelector.DrawWidth > 150;
            });
        }

        [Test]
        public void TestAdvancedStoryboardAuthoring()
        {
            DesignScreen? designScreen = null;
            StoryboardSprite? sprite = null;
            StoryboardAnimation? animation = null;
            StoryboardSampleInfo? sample = null;

            AddStep(@"switch to design mode", () => Editor.Mode.Value = EditorScreenMode.Design);
            AddUntilStep(@"design screen loaded", () => (designScreen = Editor.ChildrenOfType<DesignScreen>().SingleOrDefault())?.IsLoaded == true);

            AddStep(@"place positioned sprite", () =>
            {
                sprite = DesignStoryboardOperations.PlaceSprite(EditorBeatmap, @"sb/positioned.png", 1000, new Vector2(120, 180));
                designScreen!.StoryboardState.SelectedSprite.Value = sprite;
            });
            AddAssert(@"position preserved", () => sprite!.InitialPosition == new Vector2(120, 180));

            AddStep(@"add advanced commands", () =>
            {
                DesignStoryboardOperations.AddKeyframe(EditorBeatmap, sprite!, StoryboardCommandType.VectorScale, 1000, null, duration: 500);
                DesignStoryboardOperations.AddKeyframe(EditorBeatmap, sprite!, StoryboardCommandType.FlipHorizontal, 1100, null);
                DesignStoryboardOperations.AddKeyframe(EditorBeatmap, sprite!, StoryboardCommandType.FlipVertical, 1200, null);
                DesignStoryboardOperations.AddKeyframe(EditorBeatmap, sprite!, StoryboardCommandType.Additive, 1300, null);
            });
            AddAssert(@"vector scale added", () => sprite!.Commands.VectorScale.Single().EndTime == 1500);
            AddAssert(@"parameters added", () => sprite!.Commands.FlipH.Any() && sprite.Commands.FlipV.Any() && sprite.Commands.BlendingParameters.Any());

            AddStep(@"add loop", () => DesignStoryboardOperations.AddLoop(EditorBeatmap, sprite!, 1400, 2, 500));
            AddAssert(@"loop added", () => sprite!.LoopingGroups.Single().TotalIterations == 3);
            AddStep(@"add trigger", () => DesignStoryboardOperations.AddTrigger(EditorBeatmap, sprite!, @"Passing", 1500, 5000, 2));
            AddAssert(@"trigger added", () => sprite!.TriggerGroups.Single().TriggerName == @"Passing");

            AddStep(@"add animation and sample", () =>
            {
                animation = DesignStoryboardOperations.PlaceAnimation(EditorBeatmap, @"sb/frame.png", 2000, 8, 50, AnimationLoopType.LoopForever);
                sample = DesignStoryboardOperations.PlaceSample(EditorBeatmap, @"sb/hit.wav", 2500, 80);
            });
            AddAssert(@"animation configured", () => animation!.FrameCount == 8 && animation.FrameDelay == 50);
            AddAssert(@"sample configured", () => sample!.Volume == 80 && sample.StartTime == 2500);

            AddStep(@"change source", () =>
            {
                sprite = DesignStoryboardOperations.ChangeSpriteSource(EditorBeatmap, sprite!, StoryboardElementSource.Shared);
                designScreen!.StoryboardState.SelectedSprite.Value = sprite;
            });
            AddAssert(@"source changed with commands", () =>
                sprite!.Source == StoryboardElementSource.Shared
                && sprite.Commands.VectorScale.Any()
                && sprite.LoopingGroups.Any()
                && sprite.TriggerGroups.Any());

            AddStep(@"copy and paste", () =>
            {
                designScreen!.Copy();
                EditorClock.Seek(4000);
                designScreen.Paste();
                sprite = designScreen.StoryboardState.SelectedSprite.Value;
            });
            AddAssert(@"paste seeks commands to current time", () => sprite!.StartTime == 4000);
        }
    }
}
