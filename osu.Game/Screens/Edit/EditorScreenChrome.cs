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

        public EditorContextMenuState? ContextMenus { get; }

        public EditorScreenChrome(bool hasEditorBottomBar = true, EditorContextMenuState? contextMenus = null)
        {
            HasEditorBottomBar = hasEditorBottomBar;
            ContextMenus = contextMenus;
        }
    }

    public sealed class EditorContextMenuState
    {
        public IReadOnlyList<MenuItem> Items { get; private set; }

        public event Action? ItemsChanged;

        public EditorContextMenuState(IReadOnlyList<MenuItem> items)
        {
            Items = items;
        }

        public void Update(IReadOnlyList<MenuItem> items)
        {
            Items = items;
            ItemsChanged?.Invoke();
        }
    }
}
