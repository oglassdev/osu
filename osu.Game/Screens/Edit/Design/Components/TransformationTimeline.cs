// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Commands;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Design.Components
{
    public partial class TransformationTimeline : CompositeDrawable
    {
        private const float label_width = 150;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        private readonly StoryboardCommandType[] commandTypes =
        {
            StoryboardCommandType.Move,
            StoryboardCommandType.Scale,
            StoryboardCommandType.VectorScale,
            StoryboardCommandType.Fade,
            StoryboardCommandType.Rotate,
            StoryboardCommandType.Colour,
            StoryboardCommandType.FlipHorizontal,
            StoryboardCommandType.FlipVertical,
            StoryboardCommandType.Additive,
        };

        private readonly List<Container> trackContainers = new List<Container>();
        private OsuSpriteText selectionText = null!;
        private Container timeMarker = null!;
        private Color4[] commandColours = null!;
        private double viewStart;
        private double viewEnd = 1;
        private float zoom = 1;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            RelativeSizeAxes = Axes.Both;

            commandColours = new[]
            {
                colours.Green1,
                colours.Red1,
                colours.Red0,
                colours.Pink1,
                colours.Orange1,
                colours.Pink0,
                colours.Blue1,
                colours.Blue0,
                colours.Purple1,
            };

            var rows = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, label_width),
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 38),
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 28),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Left = 10 },
                            Child = selectionText = new TruncatingSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                Text = @"No sprite selected",
                                Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold),
                            },
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colourProvider.Background5,
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Margin = new MarginPadding { Left = 8 },
                                    Text = @"Transformation timeline",
                                    Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold),
                                },
                            },
                        },
                        Empty(),
                    },
                    new Drawable[]
                    {
                        createLabels(colourProvider),
                        createTracks(colourProvider, colours.Red1),
                        createZoomControls(),
                    },
                },
            };

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                rows,
            };

            state.SelectedSprite.BindValueChanged(_ => refresh(), true);
            beatmap.TransactionEnded += refresh;
            beatmap.SaveStateTriggered += refresh;
        }

        private Drawable createLabels(OverlayColourProvider colourProvider)
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
            };

            for (int i = 0; i < commandTypes.Length; i++)
            {
                var type = commandTypes[i];

                flow.Add(new TimelineLabel(type, commandColours[i], () => state.SelectedCommandType.Value = type)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 18,
                });
            }

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    flow,
                },
            };
        }

        private Drawable createTracks(OverlayColourProvider colourProvider, Color4 markerColour)
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
            };

            for (int i = 0; i < commandTypes.Length; i++)
            {
                var track = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 18,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = i % 2 == 0 ? colourProvider.Background3 : colourProvider.Background4,
                        },
                    },
                };

                trackContainers.Add(track);
                flow.Add(track);
            }

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                Children = new Drawable[]
                {
                    flow,
                    timeMarker = new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 2,
                        Alpha = 0,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = markerColour,
                        },
                    },
                },
            };
        }

        private Drawable createZoomControls() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Children = new Drawable[]
            {
                new IconButton
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 0.5f,
                    Icon = FontAwesome.Solid.SearchPlus,
                    Action = () =>
                    {
                        zoom = Math.Min(8, zoom * 1.5f);
                        refresh();
                    },
                },
                new IconButton
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 0.5f,
                    Icon = FontAwesome.Solid.SearchMinus,
                    Action = () =>
                    {
                        zoom = Math.Max(1, zoom / 1.5f);
                        refresh();
                    },
                },
            },
        };

        private void refresh()
        {
            foreach (var track in trackContainers)
            {
                while (track.Count > 1)
                    track.Remove(track[^1], true);
            }

            var sprite = state.SelectedSprite.Value;
            selectionText.Text = sprite?.Path ?? @"No sprite selected";

            if (sprite == null)
            {
                timeMarker.Alpha = 0;
                return;
            }

            var allCommands = commandTypes.SelectMany(type => DesignStoryboardOperations.GetCommands(sprite, type)).ToArray();
            double firstTime = allCommands.Length > 0 ? allCommands.Min(c => c.StartTime) : clock.CurrentTimeAccurate;
            double lastTime = allCommands.Length > 0 ? allCommands.Max(c => c.EndTime) : clock.CurrentTimeAccurate;
            double baseSpan = Math.Max(1000, lastTime - firstTime);
            double visibleSpan = Math.Max(250, (baseSpan + 1000) / zoom);
            double centre = (firstTime + lastTime) / 2;

            viewStart = Math.Max(0, centre - visibleSpan / 2);
            viewEnd = Math.Min(clock.TrackLength, centre + visibleSpan / 2);

            if (viewEnd - viewStart < 1)
                viewEnd = viewStart + 1;

            for (int i = 0; i < commandTypes.Length; i++)
            {
                StoryboardCommandType type = commandTypes[i];

                foreach (var command in DesignStoryboardOperations.GetCommands(sprite, type))
                    trackContainers[i].Add(new CommandSpan(command, commandColours[i], () => selectCommand(type, command.StartTime), normaliseTime));
            }

            updateTimeMarker();
        }

        private float normaliseTime(double time) => (float)((time - viewStart) / (viewEnd - viewStart));

        private void selectCommand(StoryboardCommandType type, double time)
        {
            state.SelectedCommandType.Value = type;
            clock.Seek(time);
        }

        protected override void Update()
        {
            base.Update();
            updateTimeMarker();
        }

        private void updateTimeMarker()
        {
            if (state.SelectedSprite.Value == null || clock.CurrentTimeAccurate < viewStart || clock.CurrentTimeAccurate > viewEnd)
            {
                timeMarker.Alpha = 0;
                return;
            }

            timeMarker.RelativePositionAxes = Axes.X;
            timeMarker.X = normaliseTime(clock.CurrentTimeAccurate);
            timeMarker.Alpha = 1;
        }

        protected override void Dispose(bool isDisposing)
        {
            beatmap.TransactionEnded -= refresh;
            beatmap.SaveStateTriggered -= refresh;
            base.Dispose(isDisposing);
        }

        private partial class TimelineLabel : CompositeDrawable
        {
            private readonly Action action;

            public TimelineLabel(StoryboardCommandType type, Color4 colour, Action action)
            {
                this.action = action;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(10, 0),
                        Size = new Vector2(5, 14),
                        Colour = colour,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(24, 0),
                        Text = type.ToString(),
                        Font = OsuFont.Default.With(size: 12),
                    },
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                action();
                return true;
            }
        }

        private partial class CommandSpan : CompositeDrawable
        {
            private readonly Action action;

            public CommandSpan(IStoryboardCommand command, Color4 colour, Action action, Func<double, float> normalise)
            {
                this.action = action;

                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.X;
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                X = normalise(command.StartTime);
                Width = Math.Max(0.008f, normalise(command.EndTime) - normalise(command.StartTime));
                Height = 4;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour,
                        Alpha = command.EndTime > command.StartTime ? 0.65f : 0,
                    },
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        Size = new Vector2(8),
                        Rotation = 45,
                        Colour = colour,
                    },
                    new Box
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.Centre,
                        Size = new Vector2(8),
                        Rotation = 45,
                        Colour = colour,
                        Alpha = command.EndTime > command.StartTime ? 1 : 0,
                    },
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                action();
                return true;
            }
        }
    }
}
