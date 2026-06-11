// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Components.TernaryButtons;

namespace osu.Game.Screens.Edit.Design.Components
{
    /// <summary>
    /// A compose-style ternary toggle for the storyboard toolbox, with label text hidden when the sidebar is contracted.
    /// </summary>
    internal partial class DesignToolboxTernaryButton : DrawableTernaryButton
    {
        protected override SpriteText CreateText() => new ExpandableSpriteText
        {
            Depth = -1,
            Origin = Anchor.CentreLeft,
            Anchor = Anchor.CentreLeft,
            X = 40f,
        };
    }
}
