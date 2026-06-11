// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Screens.Edit.Design.Components;
using osu.Game.Storyboards;
using osuTK;

namespace osu.Game.Screens.Edit.Design
{
    [Cached]
    public partial class DesignStoryboardState
    {
        public readonly Bindable<StoryboardSprite?> SelectedSprite = new Bindable<StoryboardSprite?>();

        public readonly Bindable<StoryboardCommandType> SelectedCommandType = new Bindable<StoryboardCommandType>(StoryboardCommandType.Move);

        public readonly Bindable<string?> PlacementPath = new Bindable<string?>();
        public readonly Bindable<Vector2?> PlacementTextureSize = new Bindable<Vector2?>();
        public readonly BindableBool Tweening = new BindableBool();
        public readonly Bindable<Easing> Easing = new Bindable<Easing>(Framework.Graphics.Easing.None);
        public readonly BindableDouble TweenDuration = new BindableDouble(500)
        {
            MinValue = 0,
            MaxValue = 10000,
        };

        public readonly BindableBool ShowBackground = new BindableBool(true);
        public readonly BindableBool ShowFail = new BindableBool(true);
        public readonly BindableBool ShowPass = new BindableBool(true);
        public readonly BindableBool ShowForeground = new BindableBool(true);
        public readonly BindableBool PreviewPassing = new BindableBool(true);

        public readonly Bindable<Vector2?> CanvasMousePosition = new Bindable<Vector2?>();

        public DesignStoryboardCanvas? Canvas { get; set; }

        internal DesignToolDialogHost? DialogHost { get; set; }
    }
}
