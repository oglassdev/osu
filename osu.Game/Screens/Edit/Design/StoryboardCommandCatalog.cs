// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Commands;
using osu.Game.Storyboards.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Design
{
    internal readonly record struct StoryboardCommandDescriptor(
        StoryboardCommandType Type,
        string DisplayName,
        IconUsage Icon,
        Func<OsuColour, Color4> GetTimelineColour,
        Func<StoryboardSprite, IEnumerable<IStoryboardCommand>> GetCommands,
        Action<StoryboardSprite, Easing, double, double, DrawableStoryboardSprite?> AddKeyframe,
        Action<StoryboardSprite, double> RemoveAtTime);

    internal static class StoryboardCommandCatalog
    {
        public static IReadOnlyList<StoryboardCommandDescriptor> All { get; } = createDescriptors();

        public static StoryboardCommandDescriptor Get(StoryboardCommandType type)
            => All.Single(descriptor => descriptor.Type == type);

        public static IEnumerable<IStoryboardCommand> GetCommands(StoryboardSprite sprite, StoryboardCommandType commandType)
            => Get(commandType).GetCommands(sprite);

        public static void AddKeyframe(
            StoryboardSprite sprite,
            StoryboardCommandType commandType,
            double time,
            DrawableStoryboardSprite? drawableSprite,
            Easing easing = Easing.None,
            double duration = 0)
        {
            RemoveAtTime(sprite, commandType, time);
            Get(commandType).AddKeyframe(sprite, easing, time, time + duration, drawableSprite);
        }

        public static void RemoveAtTime(StoryboardSprite sprite, StoryboardCommandType commandType, double time)
            => Get(commandType).RemoveAtTime(sprite, time);

        private static IReadOnlyList<StoryboardCommandDescriptor> createDescriptors() => new StoryboardCommandDescriptor[]
        {
            new(
                StoryboardCommandType.Move,
                @"Move",
                FontAwesome.Solid.ArrowsAlt,
                colours => colours.Green1,
                sprite => sprite.Commands.X.Cast<IStoryboardCommand>()
                                    .Concat(sprite.Commands.Y)
                                    .GroupBy(c => (c.StartTime, c.EndTime))
                                    .Select(g => g.First()),
                (sprite, easing, start, end, drawable) =>
                {
                    Vector2 position = drawable?.Position ?? sprite.InitialPosition;
                    sprite.Commands.AddX(easing, start, end, position.X, position.X);
                    sprite.Commands.AddY(easing, start, end, position.Y, position.Y);
                },
                (sprite, time) =>
                {
                    removeAtTime(sprite.Commands.X, time, sprite.Commands.RemoveX);
                    removeAtTime(sprite.Commands.Y, time, sprite.Commands.RemoveY);
                }),
            new(
                StoryboardCommandType.Scale,
                @"Scale",
                FontAwesome.Solid.SearchPlus,
                colours => colours.Red1,
                sprite => sprite.Commands.Scale,
                (sprite, easing, start, end, drawable) =>
                {
                    float scale = drawable?.Scale.X ?? 1;
                    sprite.Commands.AddScale(easing, start, end, scale, scale);
                },
                (sprite, time) => removeAtTime(sprite.Commands.Scale, time, sprite.Commands.RemoveScale)),
            new(
                StoryboardCommandType.VectorScale,
                @"Vector scale",
                FontAwesome.Solid.CompressArrowsAlt,
                colours => colours.Red0,
                sprite => sprite.Commands.VectorScale,
                (sprite, easing, start, end, drawable) =>
                {
                    Vector2 scale = drawable?.VectorScale ?? Vector2.One;
                    sprite.Commands.AddVectorScale(easing, start, end, scale, scale);
                },
                (sprite, time) => removeAtTime(sprite.Commands.VectorScale, time, sprite.Commands.RemoveVectorScale)),
            new(
                StoryboardCommandType.Fade,
                @"Fade",
                FontAwesome.Solid.Adjust,
                colours => colours.Pink1,
                sprite => sprite.Commands.Alpha,
                (sprite, easing, start, end, drawable) =>
                {
                    float alpha = drawable?.Alpha ?? 1;
                    sprite.Commands.AddAlpha(easing, start, end, alpha, alpha);
                },
                (sprite, time) => removeAtTime(sprite.Commands.Alpha, time, sprite.Commands.RemoveAlpha)),
            new(
                StoryboardCommandType.Rotate,
                @"Rotate",
                FontAwesome.Solid.Sync,
                colours => colours.Orange1,
                sprite => sprite.Commands.Rotation,
                (sprite, easing, start, end, drawable) =>
                {
                    float rotation = drawable?.Rotation ?? 0;
                    sprite.Commands.AddRotation(easing, start, end, rotation, rotation);
                },
                (sprite, time) => removeAtTime(sprite.Commands.Rotation, time, sprite.Commands.RemoveRotation)),
            new(
                StoryboardCommandType.Colour,
                @"Colour",
                FontAwesome.Solid.Palette,
                colours => colours.Pink0,
                sprite => sprite.Commands.Colour,
                (sprite, easing, start, end, drawable) =>
                {
                    Color4 colour = drawable?.Colour ?? Color4.White;
                    sprite.Commands.AddColour(easing, start, end, colour, colour);
                },
                (sprite, time) => removeAtTime(sprite.Commands.Colour, time, sprite.Commands.RemoveColour)),
            new(
                StoryboardCommandType.FlipHorizontal,
                @"Flip horizontal",
                FontAwesome.Solid.ArrowsAltH,
                colours => colours.Blue1,
                sprite => sprite.Commands.FlipH,
                (sprite, easing, start, end, _) => sprite.Commands.AddFlipH(Easing.None, start, end, true, true),
                (sprite, time) => removeAtTime(sprite.Commands.FlipH, time, sprite.Commands.RemoveFlipH)),
            new(
                StoryboardCommandType.FlipVertical,
                @"Flip vertical",
                FontAwesome.Solid.ArrowsAltV,
                colours => colours.Blue0,
                sprite => sprite.Commands.FlipV,
                (sprite, easing, start, end, _) => sprite.Commands.AddFlipV(Easing.None, start, end, true, true),
                (sprite, time) => removeAtTime(sprite.Commands.FlipV, time, sprite.Commands.RemoveFlipV)),
            new(
                StoryboardCommandType.Additive,
                @"Additive",
                FontAwesome.Solid.Plus,
                colours => colours.Purple1,
                sprite => sprite.Commands.BlendingParameters,
                (sprite, easing, start, end, _) =>
                {
                    var additive = new BlendingParameters
                    {
                        Source = BlendingType.SrcAlpha,
                        Destination = BlendingType.One,
                    };

                    sprite.Commands.AddBlendingParameters(Easing.None, start, end, additive, additive);
                },
                (sprite, time) => removeAtTime(sprite.Commands.BlendingParameters, time, sprite.Commands.RemoveBlendingParameters)),
        };

        private static void removeAtTime<T>(IReadOnlyList<StoryboardCommand<T>> commands, double time, Func<StoryboardCommand<T>, bool> remove)
        {
            var command = commands.FirstOrDefault(c => Math.Abs(c.StartTime - time) < DesignStoryboardOperations.KEYFRAME_TIME_EPSILON);

            if (command != null)
                remove(command);
        }
    }
}
