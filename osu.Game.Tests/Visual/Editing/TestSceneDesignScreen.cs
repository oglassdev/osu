// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Localisation;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Screens.Edit.Design.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneDesignScreen : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Test]
        public void TestLayoutVisible()
        {
            AddStep("switch to design mode", () => Editor.Mode.Value = EditorScreenMode.Design);
            AddUntilStep("layout visible", () => Editor.ChildrenOfType<DesignEditorLayout>().SingleOrDefault()?.IsPresent == true);
            AddUntilStep("context menus visible", () => Editor.ChildrenOfType<EditorContextMenuBar>().Single().IsPresent);
            AddAssert("three context menus", () => Editor.ChildrenOfType<EditorContextMenuBar>().Single().Items.Count, () => Is.EqualTo(3));
            AddAssert("context menus follow standard menus",
                () => Editor.ChildrenOfType<EditorContextMenuBar>().Single().ScreenSpaceDrawQuad.TopLeft.X,
                () => Is.EqualTo(Editor.ChildrenOfType<EditorMenuBar>().Single(menu => menu.Items.Count == 4).ScreenSpaceDrawQuad.TopRight.X).Within(1));
            AddAssert("timing menu not clipped",
                () => Editor.ChildrenOfType<EditorMenuBar.DrawableEditorBarMenuItem>().Single(item => item.Item.Text.ToString() == EditorStrings.Timing.ToString()).ScreenSpaceDrawQuad.TopRight.X,
                () => Is.LessThanOrEqualTo(Editor.ChildrenOfType<EditorContextMenuBar>().Single().ScreenSpaceDrawQuad.TopLeft.X));
            AddAssert("bottom bar hidden", () => Editor.ChildrenOfType<BottomBar>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("switch to compose mode", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("bottom bar visible", () => Editor.ChildrenOfType<BottomBar>().Single().State.Value == Visibility.Visible);
            AddAssert("context menus hidden", () => !Editor.ChildrenOfType<EditorContextMenuBar>().Single().IsPresent);
        }

        [Test]
        public void TestResizePanels()
        {
            DesignResizeHandle? handle = null;

            AddStep("switch to design mode", () => Editor.Mode.Value = EditorScreenMode.Design);
            AddUntilStep("layout visible", () => Editor.ChildrenOfType<DesignEditorLayout>().SingleOrDefault()?.IsPresent == true);

            AddStep("get left resize handle", () => handle = Editor.ChildrenOfType<DesignResizeHandle>().First(h => h.Direction == DesignResizeHandle.HandleDirection.Vertical && !h.InvertDelta));

            AddStep("drag to resize left panel", () =>
            {
                InputManager.MoveMouseTo(handle!);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(handle!.ScreenSpaceDrawQuad.TopLeft + new Vector2(50, 0));
                InputManager.ReleaseButton(MouseButton.Left);
            });
        }
    }
}
