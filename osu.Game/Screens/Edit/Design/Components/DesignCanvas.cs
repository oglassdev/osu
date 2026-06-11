// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Overlays;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class DesignCanvas : CompositeDrawable
    {
        private const float storyboard_aspect_ratio = 640f / 480f;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, IBindable<WorkingBeatmap> beatmap, OsuConfigManager config)
        {
            RelativeSizeAxes = Axes.Both;

            var showStoryboard = config.GetBindable<bool>(OsuSetting.EditorShowStoryboard);

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    FillMode = FillMode.Fit,
                    FillAspectRatio = storyboard_aspect_ratio,
                    Child = new BeatmapBackgroundWithStoryboard(beatmap.Value)
                    {
                        RelativeSizeAxes = Axes.Both,
                        ShowStoryboard = { BindTarget = showStoryboard },
                    },
                },
            };
        }
    }
}
