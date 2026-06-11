// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.UserInterface;

namespace osu.Game.Screens.Edit
{
    public sealed class EditorScreenChrome
    {
        public static readonly EditorScreenChrome DEFAULT = new EditorScreenChrome();

        public bool HasEditorBottomBar { get; }

        public IReadOnlyList<MenuItem> ContextMenuItems { get; }

        public EditorScreenChrome(bool hasEditorBottomBar = true, IReadOnlyList<MenuItem>? contextMenuItems = null)
        {
            HasEditorBottomBar = hasEditorBottomBar;
            ContextMenuItems = contextMenuItems ?? Array.Empty<MenuItem>();
        }
    }
}
