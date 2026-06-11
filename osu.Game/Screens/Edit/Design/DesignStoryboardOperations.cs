// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Commands;
using osu.Game.Storyboards.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Design
{
    internal static class DesignStoryboardOperations
    {
        public const double KEYFRAME_TIME_EPSILON = 0.5;

        /// <summary>
        /// Sprites larger than this on their longest edge are scaled down when first placed.
        /// </summary>
        private const float default_max_sprite_dimension = 240f;

        public static readonly string[] EDITABLE_LAYERS = { @"Background", @"Fail", @"Pass", @"Foreground" };

        public static string? FindLayerName(Storyboard storyboard, StoryboardSprite sprite)
        {
            foreach (var layer in storyboard.Layers)
            {
                if (layer.Elements.Contains(sprite))
                    return layer.Name;
            }

            return null;
        }

        public static Vector2 ComputeDefaultVectorScale(Vector2 textureSize)
        {
            if (textureSize.X <= 0 || textureSize.Y <= 0)
                return Vector2.One;

            float maxDimension = Math.Max(textureSize.X, textureSize.Y);

            if (maxDimension <= default_max_sprite_dimension)
                return Vector2.One;

            float factor = default_max_sprite_dimension / maxDimension;
            return new Vector2(factor);
        }

        public static StoryboardSprite PlaceSprite(
            EditorBeatmap beatmap,
            string path,
            double time,
            Vector2? position = null,
            StoryboardElementSource source = StoryboardElementSource.Beatmap,
            Vector2? textureSize = null)
        {
            beatmap.BeginChange();

            var sprite = new StoryboardSprite(source, path, Anchor.Centre, position ?? new Vector2(320, 240));
            sprite.Commands.AddAlpha(Easing.None, time, time, 1, 1);

            if (textureSize is Vector2 size)
            {
                Vector2 vectorScale = ComputeDefaultVectorScale(size);

                if (vectorScale != Vector2.One)
                    sprite.Commands.AddVectorScale(Easing.None, time, time, vectorScale, vectorScale);
            }

            beatmap.Storyboard.GetLayer(@"Foreground").Add(sprite);

            beatmap.EndChange();
            return sprite;
        }

        public static Vector2 GetSpriteDisplaySize(DrawableStoryboardSprite drawable)
            => new Vector2(drawable.DrawWidth, drawable.DrawHeight);

        public static void SetSpriteDisplayWidth(DrawableStoryboardSprite drawable, float width)
        {
            if (drawable.Size.X <= 0)
                return;

            float scale = MathF.Abs(drawable.Scale.X);

            if (scale <= 0)
                return;

            drawable.VectorScale = new Vector2(width / (drawable.Size.X * scale), drawable.VectorScale.Y);
        }

        public static void SetSpriteDisplayHeight(DrawableStoryboardSprite drawable, float height)
        {
            if (drawable.Size.Y <= 0)
                return;

            float scale = MathF.Abs(drawable.Scale.Y);

            if (scale <= 0)
                return;

            drawable.VectorScale = new Vector2(drawable.VectorScale.X, height / (drawable.Size.Y * scale));
        }

        public static void ApplySpriteTransform(EditorBeatmap beatmap, StoryboardSprite sprite, DrawableStoryboardSprite drawable, double time)
        {
            beatmap.BeginChange();
            applySpriteTransform(sprite, drawable, time);
            beatmap.EndChange();
        }

        internal static void applySpriteTransform(StoryboardSprite sprite, DrawableStoryboardSprite drawable, double time)
        {
            removeKeyframeAtTime(sprite, StoryboardCommandType.Move, time);
            sprite.Commands.AddX(Easing.None, time, time, drawable.Position.X, drawable.Position.X);
            sprite.Commands.AddY(Easing.None, time, time, drawable.Position.Y, drawable.Position.Y);

            removeKeyframeAtTime(sprite, StoryboardCommandType.VectorScale, time);
            sprite.Commands.AddVectorScale(Easing.None, time, time, drawable.VectorScale, drawable.VectorScale);

            removeKeyframeAtTime(sprite, StoryboardCommandType.Scale, time);

            if (!Precision.AlmostEquals(drawable.Scale.X, 1))
                sprite.Commands.AddScale(Easing.None, time, time, drawable.Scale.X, drawable.Scale.X);

            removeKeyframeAtTime(sprite, StoryboardCommandType.Rotate, time);

            if (!Precision.AlmostEquals(drawable.Rotation, 0))
                sprite.Commands.AddRotation(Easing.None, time, time, drawable.Rotation, drawable.Rotation);

            removeKeyframeAtTime(sprite, StoryboardCommandType.FlipHorizontal, time);

            if (drawable.FlipH)
                sprite.Commands.AddFlipH(Easing.None, time, time, true, true);

            removeKeyframeAtTime(sprite, StoryboardCommandType.FlipVertical, time);

            if (drawable.FlipV)
                sprite.Commands.AddFlipV(Easing.None, time, time, true, true);
        }

        public static void DeleteSprite(EditorBeatmap beatmap, StoryboardSprite sprite)
        {
            beatmap.BeginChange();

            foreach (var layer in beatmap.Storyboard.Layers)
                layer.Elements.Remove(sprite);

            beatmap.EndChange();
        }

        public static void DeleteElement(EditorBeatmap beatmap, IStoryboardElement element)
        {
            beatmap.BeginChange();

            foreach (var layer in beatmap.Storyboard.Layers)
                layer.Elements.Remove(element);

            beatmap.EndChange();
        }

        public static void DeleteAsset(EditorBeatmap beatmap, BeatmapManager beatmaps, BeatmapSetInfo setInfo, string path)
        {
            beatmap.BeginChange();

            foreach (var layer in beatmap.Storyboard.Layers)
                layer.Elements.RemoveAll(element => element is StoryboardSprite sprite && sprite.Path == path);

            beatmap.EndChange();

            var file = setInfo.GetFile(path);

            if (file != null)
                beatmaps.DeleteFile(setInfo, file);
        }

        public static bool RenameAsset(EditorBeatmap beatmap, BeatmapManager beatmaps, IWorkingBeatmap workingBeatmap, BeatmapSetInfo setInfo, string oldPath, string newPath)
        {
            newPath = newPath.ToStandardisedPath();
            oldPath = oldPath.ToStandardisedPath();

            if (string.IsNullOrWhiteSpace(newPath) || oldPath == newPath)
                return false;

            if (setInfo.GetFile(newPath) != null)
                return false;

            var existingFile = setInfo.GetFile(oldPath);

            if (existingFile == null)
                return false;

            string? storagePath = setInfo.GetPathForFile(oldPath);

            if (storagePath == null)
                return false;

            using var stream = workingBeatmap.GetStream(storagePath);
            beatmaps.AddFile(setInfo, stream, newPath);
            beatmaps.DeleteFile(setInfo, existingFile);

            beatmap.BeginChange();

            foreach (var layer in beatmap.Storyboard.Layers)
            {
                for (int i = 0; i < layer.Elements.Count; i++)
                {
                    if (layer.Elements[i] is StoryboardSprite sprite && sprite.Path == oldPath)
                        layer.Elements[i] = createSpriteClone(sprite, newPath: newPath);
                }
            }

            beatmap.EndChange();
            return true;
        }

        public static void ChangeSpriteLayer(EditorBeatmap beatmap, StoryboardSprite sprite, string newLayerName)
        {
            string? currentLayer = FindLayerName(beatmap.Storyboard, sprite);

            if (currentLayer == newLayerName)
                return;

            beatmap.BeginChange();

            foreach (var layer in beatmap.Storyboard.Layers)
                layer.Elements.Remove(sprite);

            beatmap.Storyboard.GetLayer(newLayerName).Add(sprite);

            beatmap.EndChange();
        }

        public static void SetSpriteOrigin(EditorBeatmap beatmap, StoryboardSprite sprite, Anchor origin)
        {
            if (sprite.Origin == origin)
                return;

            beatmap.BeginChange();
            sprite.Origin = origin;
            beatmap.EndChange();
        }

        public static void AddKeyframe(
            EditorBeatmap beatmap,
            StoryboardSprite sprite,
            StoryboardCommandType commandType,
            double time,
            DrawableStoryboardSprite? drawableSprite,
            Easing easing = Easing.None,
            double duration = 0)
        {
            beatmap.BeginChange();

            removeKeyframeAtTime(sprite, commandType, time);
            double endTime = time + duration;

            switch (commandType)
            {
                case StoryboardCommandType.Move:
                {
                    Vector2 position = getSpritePosition(sprite, drawableSprite);
                    sprite.Commands.AddX(easing, time, endTime, position.X, position.X);
                    sprite.Commands.AddY(easing, time, endTime, position.Y, position.Y);
                    break;
                }

                case StoryboardCommandType.Scale:
                {
                    float scale = getSpriteScale(drawableSprite);
                    sprite.Commands.AddScale(easing, time, endTime, scale, scale);
                    break;
                }

                case StoryboardCommandType.VectorScale:
                {
                    Vector2 scale = drawableSprite?.VectorScale ?? Vector2.One;
                    sprite.Commands.AddVectorScale(easing, time, endTime, scale, scale);
                    break;
                }

                case StoryboardCommandType.Fade:
                {
                    float alpha = drawableSprite?.Alpha ?? 1;
                    sprite.Commands.AddAlpha(easing, time, endTime, alpha, alpha);
                    break;
                }

                case StoryboardCommandType.Rotate:
                {
                    float rotation = getSpriteRotation(drawableSprite);
                    sprite.Commands.AddRotation(easing, time, endTime, rotation, rotation);
                    break;
                }

                case StoryboardCommandType.Colour:
                {
                    Color4 colour = getSpriteColour(drawableSprite);
                    sprite.Commands.AddColour(easing, time, endTime, colour, colour);
                    break;
                }

                case StoryboardCommandType.FlipHorizontal:
                    sprite.Commands.AddFlipH(Easing.None, time, endTime, true, true);
                    break;

                case StoryboardCommandType.FlipVertical:
                    sprite.Commands.AddFlipV(Easing.None, time, endTime, true, true);
                    break;

                case StoryboardCommandType.Additive:
                {
                    var additive = new BlendingParameters
                    {
                        Source = BlendingType.SrcAlpha,
                        Destination = BlendingType.One,
                    };

                    sprite.Commands.AddBlendingParameters(Easing.None, time, endTime, additive, additive);
                    break;
                }
            }

            beatmap.EndChange();
        }

        public static void RemoveKeyframe(EditorBeatmap beatmap, StoryboardSprite sprite, StoryboardCommandType commandType, double time)
        {
            beatmap.BeginChange();
            removeKeyframeAtTime(sprite, commandType, time);
            beatmap.EndChange();
        }

        public static IEnumerable<IStoryboardCommand> GetCommands(StoryboardSprite sprite, StoryboardCommandType commandType) => commandType switch
        {
            StoryboardCommandType.Move => sprite.Commands.X.Cast<IStoryboardCommand>()
                                                   .Concat(sprite.Commands.Y)
                                                   .GroupBy(c => (c.StartTime, c.EndTime))
                                                   .Select(g => g.First()),
            StoryboardCommandType.Scale => sprite.Commands.Scale,
            StoryboardCommandType.VectorScale => sprite.Commands.VectorScale,
            StoryboardCommandType.Fade => sprite.Commands.Alpha,
            StoryboardCommandType.Rotate => sprite.Commands.Rotation,
            StoryboardCommandType.Colour => sprite.Commands.Colour,
            StoryboardCommandType.FlipHorizontal => sprite.Commands.FlipH,
            StoryboardCommandType.FlipVertical => sprite.Commands.FlipV,
            StoryboardCommandType.Additive => sprite.Commands.BlendingParameters,
            _ => Array.Empty<IStoryboardCommand>(),
        };

        public static double? FindAdjacentKeyframeTime(StoryboardSprite sprite, StoryboardCommandType commandType, double time, int direction)
        {
            var times = GetCommands(sprite, commandType)
                        .SelectMany(c => c.EndTime > c.StartTime ? new[] { c.StartTime, c.EndTime } : new[] { c.StartTime })
                        .Distinct()
                        .OrderBy(t => t)
                        .ToArray();

            if (direction < 0)
            {
                for (int i = times.Length - 1; i >= 0; i--)
                {
                    if (times[i] < time - KEYFRAME_TIME_EPSILON)
                        return times[i];
                }
            }
            else
            {
                foreach (double keyframeTime in times)
                {
                    if (keyframeTime > time + KEYFRAME_TIME_EPSILON)
                        return keyframeTime;
                }
            }

            return null;
        }

        public static StoryboardAnimation PlaceAnimation(
            EditorBeatmap beatmap,
            string path,
            double time,
            int frameCount,
            double frameDelay,
            AnimationLoopType loopType,
            StoryboardElementSource source = StoryboardElementSource.Beatmap,
            Vector2? textureSize = null)
        {
            beatmap.BeginChange();

            var animation = new StoryboardAnimation(source, path, Anchor.Centre, new Vector2(320, 240), frameCount, frameDelay, loopType);
            animation.Commands.AddAlpha(Easing.None, time, time, 1, 1);

            if (textureSize is Vector2 size)
            {
                Vector2 vectorScale = ComputeDefaultVectorScale(size);

                if (vectorScale != Vector2.One)
                    animation.Commands.AddVectorScale(Easing.None, time, time, vectorScale, vectorScale);
            }

            beatmap.Storyboard.GetLayer(@"Foreground").Add(animation);

            beatmap.EndChange();
            return animation;
        }

        public static StoryboardSampleInfo PlaceSample(
            EditorBeatmap beatmap,
            string path,
            double time,
            int volume,
            StoryboardElementSource source = StoryboardElementSource.Beatmap)
        {
            beatmap.BeginChange();

            var sample = new StoryboardSampleInfo(source, path, time, volume);
            beatmap.Storyboard.GetLayer(@"Foreground").Add(sample);

            beatmap.EndChange();
            return sample;
        }

        public static void AddLoop(EditorBeatmap beatmap, StoryboardSprite sprite, double time, int repeatCount, double duration)
        {
            beatmap.BeginChange();

            var loop = sprite.AddLoopingGroup(time, repeatCount);
            loop.AddAlpha(Easing.None, 0, duration, 1, 1);

            beatmap.EndChange();
        }

        public static void AddTrigger(
            EditorBeatmap beatmap,
            StoryboardSprite sprite,
            string triggerName,
            double startTime,
            double endTime,
            int groupNumber)
        {
            beatmap.BeginChange();

            var trigger = sprite.AddTriggerGroup(triggerName, startTime, endTime, groupNumber);
            trigger.AddAlpha(Easing.None, 0, 0, 1, 1);

            beatmap.EndChange();
        }

        public static StoryboardSprite CloneSprite(
            EditorBeatmap beatmap,
            StoryboardSprite sprite,
            double timeOffset = 0,
            StoryboardElementSource? source = null,
            string? targetLayer = null)
        {
            StoryboardSprite clone = createSpriteClone(sprite, source, timeOffset);
            string layerName = targetLayer ?? FindLayerName(beatmap.Storyboard, sprite) ?? @"Foreground";

            beatmap.BeginChange();
            beatmap.Storyboard.GetLayer(layerName).Add(clone);
            beatmap.EndChange();

            return clone;
        }

        public static StoryboardSprite ChangeSpriteSource(EditorBeatmap beatmap, StoryboardSprite sprite, StoryboardElementSource source)
        {
            if (sprite.Source == source)
                return sprite;

            StoryboardSprite clone = createSpriteClone(sprite, source);
            string layerName = FindLayerName(beatmap.Storyboard, sprite) ?? @"Foreground";

            beatmap.BeginChange();
            beatmap.Storyboard.GetLayer(layerName).Elements.Remove(sprite);
            beatmap.Storyboard.GetLayer(layerName).Add(clone);
            beatmap.EndChange();

            return clone;
        }

        private static StoryboardSprite createSpriteClone(StoryboardSprite sprite, StoryboardElementSource? source = null, double timeOffset = 0, string? newPath = null)
        {
            string path = newPath ?? sprite.Path;

            StoryboardSprite clone = sprite switch
            {
                StoryboardAnimation animation => new StoryboardAnimation(
                    source ?? sprite.Source,
                    path,
                    animation.Origin,
                    animation.InitialPosition,
                    animation.FrameCount,
                    animation.FrameDelay,
                    animation.LoopType),
                _ => new StoryboardSprite(source ?? sprite.Source, path, sprite.Origin, sprite.InitialPosition),
            };

            copyCommands(sprite.Commands.AllCommands, clone.Commands, timeOffset);

            foreach (var sourceLoop in sprite.LoopingGroups)
            {
                var firstLoopingCommand = sourceLoop.AllCommands.OfType<IStoryboardLoopingCommand>().FirstOrDefault();

                if (firstLoopingCommand == null)
                    continue;

                double loopStart = ((IStoryboardCommand)firstLoopingCommand).StartTime - firstLoopingCommand.OriginalCommand.StartTime + timeOffset;
                var targetLoop = clone.AddLoopingGroup(loopStart, sourceLoop.TotalIterations - 1);
                copyCommands(sourceLoop.AllCommands.OfType<IStoryboardLoopingCommand>().Select(c => c.OriginalCommand), targetLoop, 0);
            }

            foreach (var sourceTrigger in sprite.TriggerGroups)
            {
                var targetTrigger = clone.AddTriggerGroup(
                    sourceTrigger.TriggerName,
                    sourceTrigger.TriggerStartTime + timeOffset,
                    sourceTrigger.TriggerEndTime + timeOffset,
                    sourceTrigger.GroupNumber);

                copyCommands(sourceTrigger.AllCommands, targetTrigger, timeOffset);
            }

            return clone;
        }

        private static void copyCommands(IEnumerable<IStoryboardCommand> commands, StoryboardCommandGroup target, double timeOffset)
        {
            foreach (var command in commands)
            {
                double startTime = command.StartTime + timeOffset;
                double endTime = command.EndTime + timeOffset;

                switch (command)
                {
                    case StoryboardXCommand x:
                        target.AddX(x.Easing, startTime, endTime, x.StartValue, x.EndValue);
                        break;

                    case StoryboardYCommand y:
                        target.AddY(y.Easing, startTime, endTime, y.StartValue, y.EndValue);
                        break;

                    case StoryboardScaleCommand scale:
                        target.AddScale(scale.Easing, startTime, endTime, scale.StartValue, scale.EndValue);
                        break;

                    case StoryboardVectorScaleCommand vectorScale:
                        target.AddVectorScale(vectorScale.Easing, startTime, endTime, vectorScale.StartValue, vectorScale.EndValue);
                        break;

                    case StoryboardAlphaCommand alpha:
                        target.AddAlpha(alpha.Easing, startTime, endTime, alpha.StartValue, alpha.EndValue);
                        break;

                    case StoryboardRotationCommand rotation:
                        target.AddRotation(rotation.Easing, startTime, endTime, rotation.StartValue, rotation.EndValue);
                        break;

                    case StoryboardColourCommand colour:
                        target.AddColour(colour.Easing, startTime, endTime, colour.StartValue, colour.EndValue);
                        break;

                    case StoryboardFlipHCommand flipH:
                        target.AddFlipH(flipH.Easing, startTime, endTime, flipH.StartValue, flipH.EndValue);
                        break;

                    case StoryboardFlipVCommand flipV:
                        target.AddFlipV(flipV.Easing, startTime, endTime, flipV.StartValue, flipV.EndValue);
                        break;

                    case StoryboardBlendingParametersCommand blending:
                        target.AddBlendingParameters(blending.Easing, startTime, endTime, blending.StartValue, blending.EndValue);
                        break;
                }
            }
        }

        private static void removeKeyframeAtTime(StoryboardSprite sprite, StoryboardCommandType commandType, double time)
        {
            switch (commandType)
            {
                case StoryboardCommandType.Move:
                    removeAtTime(sprite.Commands.X, time, sprite.Commands.RemoveX);
                    removeAtTime(sprite.Commands.Y, time, sprite.Commands.RemoveY);
                    break;

                case StoryboardCommandType.Scale:
                    removeAtTime(sprite.Commands.Scale, time, sprite.Commands.RemoveScale);
                    break;

                case StoryboardCommandType.VectorScale:
                    removeAtTime(sprite.Commands.VectorScale, time, sprite.Commands.RemoveVectorScale);
                    break;

                case StoryboardCommandType.Fade:
                    removeAtTime(sprite.Commands.Alpha, time, sprite.Commands.RemoveAlpha);
                    break;

                case StoryboardCommandType.Rotate:
                    removeAtTime(sprite.Commands.Rotation, time, sprite.Commands.RemoveRotation);
                    break;

                case StoryboardCommandType.Colour:
                    removeAtTime(sprite.Commands.Colour, time, sprite.Commands.RemoveColour);
                    break;

                case StoryboardCommandType.FlipHorizontal:
                    removeAtTime(sprite.Commands.FlipH, time, sprite.Commands.RemoveFlipH);
                    break;

                case StoryboardCommandType.FlipVertical:
                    removeAtTime(sprite.Commands.FlipV, time, sprite.Commands.RemoveFlipV);
                    break;

                case StoryboardCommandType.Additive:
                    removeAtTime(sprite.Commands.BlendingParameters, time, sprite.Commands.RemoveBlendingParameters);
                    break;
            }
        }

        private static Vector2 getSpritePosition(StoryboardSprite sprite, DrawableStoryboardSprite? drawableSprite)
            => drawableSprite?.Position ?? sprite.InitialPosition;

        private static float getSpriteScale(DrawableStoryboardSprite? drawableSprite)
            => drawableSprite?.Scale.X ?? 1;

        private static float getSpriteRotation(DrawableStoryboardSprite? drawableSprite)
            => drawableSprite?.Rotation ?? 0;

        private static Color4 getSpriteColour(DrawableStoryboardSprite? drawableSprite)
            => drawableSprite?.Colour ?? Color4.White;

        private static void removeAtTime<T>(System.Collections.Generic.IReadOnlyList<StoryboardCommand<T>> commands, double time, Func<StoryboardCommand<T>, bool> remove)
        {
            var command = commands.FirstOrDefault(c => Math.Abs(c.StartTime - time) < KEYFRAME_TIME_EPSILON);

            if (command != null)
                remove(command);
        }
    }
}
