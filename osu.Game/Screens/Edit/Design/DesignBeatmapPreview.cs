// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;
namespace osu.Game.Screens.Edit.Design
{
    /// <summary>
    /// Renders the beatmap playfield and hit objects at the current editor time as a read-only preview.
    /// </summary>
    internal partial class DesignBeatmapPreview : CompositeDrawable
    {
        private static readonly MethodInfo add_hit_object_method =
            typeof(DrawableRuleset<>).GetMethod(nameof(DrawableRuleset<HitObject>.AddHitObject), new[] { typeof(HitObject) })!;

        private static readonly MethodInfo remove_hit_object_method =
            typeof(DrawableRuleset<>).GetMethod(nameof(DrawableRuleset<HitObject>.RemoveHitObject), new[] { typeof(HitObject) })!;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

        [Resolved]
        private IEditorChangeHandler? changeHandler { get; set; }

        private Container backgroundLayer = null!;
        private Container rulesetLayer = null!;
        private DrawableRuleset? drawableRuleset;
        private int loadGeneration;

        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                backgroundLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
                rulesetLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            loadBackground();
            loadRuleset();

            if (drawableRuleset == null)
                return;

            beatmap.HitObjectAdded += onHitObjectAdded;
            beatmap.HitObjectRemoved += onHitObjectRemoved;
            beatmap.BeatmapReprocessed += onBeatmapReprocessed;

            if (changeHandler != null)
                changeHandler.OnStateChange += onStateChange;
            else
                beatmap.HitObjectUpdated += onHitObjectUpdated;
        }

        private void loadBackground()
        {
            LoadComponentAsync(new BeatmapBackgroundSprite(workingBeatmap.Value)
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Fill,
            }, background =>
            {
                if (background.Texture == null)
                    return;

                backgroundLayer.Child = background;
            });
        }

        private void loadRuleset()
        {
            int generation = ++loadGeneration;

            try
            {
                Ruleset ruleset = beatmap.PlayableBeatmap.BeatmapInfo.Ruleset.CreateInstance();
                Mod autoplay = ruleset.GetAutoplayMod();

                var rulesetDrawable = ruleset.CreateDrawableRulesetWith(beatmap.PlayableBeatmap, new Mod[] { autoplay });
                rulesetDrawable.FrameStablePlayback = false;
                rulesetDrawable.Playfield.DisplayJudgements.Value = false;
                rulesetDrawable.Clock = clock;
                rulesetDrawable.ProcessCustomClock = false;
                rulesetDrawable.RelativeSizeAxes = Axes.Both;

                LoadComponentAsync(rulesetDrawable, loaded =>
                {
                    if (generation != loadGeneration)
                    {
                        loaded.Expire();
                        return;
                    }

                    if (drawableRuleset != null)
                        rulesetLayer.Remove(drawableRuleset, false);

                    drawableRuleset = loaded;
                    rulesetLayer.Child = loaded;

                    regenerateAutoplay();
                });
            }
            catch (Exception)
            {
                // Unsupported ruleset or load failure.
            }
        }

        private void onBeatmapReprocessed()
        {
            Schedule(() =>
            {
                loadBackground();
                loadRuleset();
            });
        }

        private void onHitObjectAdded(HitObject hitObject)
        {
            invokeHitObjectMethod(add_hit_object_method, hitObject);
            drawableRuleset?.Playfield.PostProcess();
        }

        private void onHitObjectRemoved(HitObject hitObject)
        {
            invokeHitObjectMethod(remove_hit_object_method, hitObject);
            drawableRuleset?.Playfield.PostProcess();
        }

        private void onHitObjectUpdated(HitObject _) => Schedule(regenerateAutoplay);

        private void onStateChange() => Schedule(regenerateAutoplay);

        private void regenerateAutoplay()
        {
            if (drawableRuleset == null)
                return;

            var autoplayMod = drawableRuleset.Mods.OfType<ModAutoplay>().SingleOrDefault();

            if (autoplayMod != null)
                drawableRuleset.SetReplayScore(autoplayMod.CreateScoreFromReplayData(beatmap.PlayableBeatmap, drawableRuleset.Mods));
        }

        private void invokeHitObjectMethod(MethodInfo method, HitObject hitObject)
        {
            if (drawableRuleset == null)
                return;

            method.Invoke(drawableRuleset, new object[] { hitObject });
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (beatmap.IsNotNull())
            {
                beatmap.HitObjectAdded -= onHitObjectAdded;
                beatmap.HitObjectRemoved -= onHitObjectRemoved;
                beatmap.HitObjectUpdated -= onHitObjectUpdated;
                beatmap.BeatmapReprocessed -= onBeatmapReprocessed;
            }

            if (changeHandler != null)
                changeHandler.OnStateChange -= onStateChange;
        }
    }
}
