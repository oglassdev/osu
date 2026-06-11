// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Screens.Edit.Design.Components;

namespace osu.Game.Screens.Edit.Design
{
    public partial class DesignScreen : EditorScreen
    {
        public override EditorScreenChrome Chrome { get; }

        public DesignScreen()
            : base(EditorScreenMode.Design)
        {
            Chrome = new EditorScreenChrome(hasEditorBottomBar: false, contextMenus: new EditorContextMenuState(new MenuItem[]
            {
                new MenuItem(EditorDesignStrings.Tools)
                {
                    Items = new MenuItem[]
                    {
                        new EditorMenuItem(EditorDesignStrings.Placeholder),
                    },
                },
                new MenuItem(EditorDesignStrings.Animate)
                {
                    Items = new MenuItem[]
                    {
                        new EditorMenuItem(EditorDesignStrings.Placeholder),
                    },
                },
                new MenuItem(EditorDesignStrings.Resources)
                {
                    Items = new MenuItem[]
                    {
                        new EditorMenuItem(EditorDesignStrings.Placeholder),
                    },
                },
            }));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new DesignEditorLayout
            {
                RelativeSizeAxes = Axes.Both,
            };
        }
    }
}
