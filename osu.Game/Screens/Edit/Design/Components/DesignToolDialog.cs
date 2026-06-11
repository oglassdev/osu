// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Design.Components
{
    /// <summary>
    /// A centred floating panel for storyboard editor tools that need more space than a toolbox popover.
    /// </summary>
    internal partial class DesignToolDialog : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        public const float WIDTH = 480;
        public const float HEIGHT = 560;

        public event Action? Dismissed;

        private readonly Container contentContainer;
        private readonly OsuSpriteText titleText;
        private readonly Box panelBackground;

        protected override bool StartHidden => true;

        public DesignToolDialog()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new DismissBackdrop
                {
                    RelativeSizeAxes = Axes.Both,
                    Action = Hide,
                },
                new DialogBody
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(WIDTH, HEIGHT),
                    Masking = true,
                    CornerRadius = 10,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Offset = new Vector2(0, 4),
                        Radius = 8,
                        Colour = Colour4.Black.Opacity(0.35f),
                    },
                    Children = new Drawable[]
                    {
                        panelBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 45,
                                    Children = new Drawable[]
                                    {
                                        titleText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Position = new Vector2(20, 0),
                                            Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 20),
                                        },
                                        new IconButton
                                        {
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            Position = new Vector2(-15, 0),
                                            Icon = FontAwesome.Solid.Times,
                                            Action = Hide,
                                        },
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 20, Bottom = 20 },
                                    Child = new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        ScrollbarOverlapsContent = false,
                                        Child = contentContainer = new Container
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            panelBackground.Colour = colourProvider.Background4;
        }

        public void Display(LocalisableString title, Drawable content)
        {
            titleText.Text = title;

            contentContainer.Clear();
            contentContainer.Add(content);

            Show();
        }

        public new void Hide()
        {
            base.Hide();
            Dismissed?.Invoke();
        }

        protected override void PopIn() => this.FadeIn(200, Easing.OutQuint);

        protected override void PopOut() => this.FadeOut(150, Easing.OutQuint);

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat || State.Value == Visibility.Hidden)
                return false;

            if (e.Action == GlobalAction.Back)
            {
                Hide();
                return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        private partial class DismissBackdrop : OsuClickableContainer
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                AddInternal(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.5f,
                });
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (e.Button != MouseButton.Left)
                    return false;

                return base.OnClick(e);
            }
        }

        /// <summary>
        /// Blocks left-clicks from falling through to the dismiss backdrop while allowing right-clicks for context menus.
        /// </summary>
        private partial class DialogBody : Container
        {
            protected override bool OnMouseDown(MouseDownEvent e) => e.Button == MouseButton.Left;

            protected override bool OnClick(ClickEvent e) => e.Button == MouseButton.Left;
        }
    }
}
