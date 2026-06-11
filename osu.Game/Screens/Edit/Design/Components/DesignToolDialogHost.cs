// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;

namespace osu.Game.Screens.Edit.Design.Components
{
    /// <summary>
    /// Hosts a single <see cref="DesignToolDialog"/> above the storyboard editor.
    /// </summary>
    internal partial class DesignToolDialogHost : CompositeDrawable
    {
        private readonly DesignToolDialog dialog;

        private BindableBool? activeSelection;
        private bool suppressSelectionReset;

        public DesignToolDialogHost()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = dialog = new DesignToolDialog();
            dialog.Dismissed += onDialogDismissed;
        }

        public void Show(LocalisableString title, Func<Drawable> createContent, BindableBool selection)
        {
            if (activeSelection == selection && dialog.State.Value == Visibility.Visible)
            {
                HideDialog();
                return;
            }

            suppressSelectionReset = true;

            if (activeSelection != null)
                activeSelection.Value = false;

            suppressSelectionReset = false;

            activeSelection = selection;
            selection.Value = true;

            dialog.Display(title, createContent());
        }

        public void HideDialog()
        {
            if (dialog.State.Value == Visibility.Hidden)
                return;

            dialog.Hide();
        }

        private void onDialogDismissed()
        {
            if (suppressSelectionReset)
                return;

            if (activeSelection != null)
            {
                suppressSelectionReset = true;
                activeSelection.Value = false;
                suppressSelectionReset = false;
                activeSelection = null;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            dialog.Dismissed -= onDialogDismissed;
            base.Dispose(isDisposing);
        }
    }
}
