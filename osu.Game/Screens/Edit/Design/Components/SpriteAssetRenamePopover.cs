// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Framework.Extensions;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;
using osuTK;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class SpriteAssetRenamePopover : OsuPopover
    {
        private readonly string currentPath;
        private readonly Action<string> onCommit;

        private FocusedTextBox textBox = null!;
        private RoundedButton renameButton = null!;

        public SpriteAssetRenamePopover(string currentPath, Action<string> onCommit)
        {
            this.currentPath = currentPath;
            this.onCommit = onCommit;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                Width = 300,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    textBox = new FocusedTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        PlaceholderText = CommonStrings.Name,
                        FontSize = OsuFont.DEFAULT_FONT_SIZE,
                        SelectAllOnFocus = true,
                    },
                    renameButton = new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 40,
                        Text = WebCommonStrings.ButtonsSave,
                    },
                },
            };

            renameButton.Action = commit;
            textBox.OnCommit += (_, _) => commit();
        }

        protected override void PopIn()
        {
            textBox.Text = Path.GetFileNameWithoutExtension(currentPath);
            textBox.TakeFocus();
            base.PopIn();
        }

        private void commit()
        {
            string newFileName = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newFileName))
                return;

            string extension = Path.GetExtension(currentPath);
            string? directory = Path.GetDirectoryName(currentPath);
            string newPath = string.IsNullOrEmpty(directory)
                ? newFileName + extension
                : Path.Combine(directory, newFileName + extension).Replace('\\', '/');

            onCommit(newPath.ToStandardisedPath());
            this.HidePopover();
        }
    }
}
