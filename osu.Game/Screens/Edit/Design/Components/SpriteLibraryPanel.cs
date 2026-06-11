// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets.Edit;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Design;
using osu.Game.Storyboards;
using osu.Game.Utils;
using osuTK;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Design.Components
{
    internal partial class SpriteLibraryPanel : CompositeDrawable
    {
        private const int grid_columns = 4;
        private const float cell_size = 120;

        public SpriteLibraryPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private DesignStoryboardState state { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private FillFlowContainer contentFlow = null!;
        private Container thumbnailContainer = null!;
        private BasicSearchTextBox searchBox = null!;
        private readonly Bindable<string> searchText = new Bindable<string>(string.Empty);
        private readonly Bindable<FileInfo?> selectedFile = new Bindable<FileInfo?>();

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = contentFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 12),
                Children = new Drawable[]
                {
                    new FormFileSelector(SupportedExtensions.IMAGE_EXTENSIONS)
                    {
                        Caption = @"Import image",
                        PlaceholderText = @"Choose or drop an image",
                        Current = selectedFile,
                    },
                    searchBox = new BasicSearchTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        PlaceholderText = @"Search sprites...",
                    },
                    thumbnailContainer = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                },
            };

            searchBox.Current.BindTarget = searchText;
            searchText.BindValueChanged(_ => refreshThumbnails(), true);

            selectedFile.BindValueChanged(selection =>
            {
                if (selection.NewValue == null)
                    return;

                importImage(selection.NewValue);
                selectedFile.Value = null;
            });

            refreshThumbnails();
            workingBeatmap.BindValueChanged(_ => refreshThumbnails());
        }

        private void importImage(FileInfo file)
        {
            using var stream = file.OpenRead();
            beatmaps.AddFile(workingBeatmap.Value.BeatmapSetInfo, stream, file.Name);
            refreshThumbnails();
            placeSprite(file.Name);
        }

        private void refreshThumbnails()
        {
            thumbnailContainer.Clear();

            string[] filenames = getImageFilenames().ToArray();

            if (filenames.Length == 0)
            {
                thumbnailContainer.Child = new OsuSpriteText
                {
                    Text = string.IsNullOrWhiteSpace(searchText.Value)
                        ? @"No images yet. Import one to create a sprite."
                        : @"No images match your search.",
                    RelativeSizeAxes = Axes.X,
                };
                return;
            }

            thumbnailContainer.Child = createThumbnailGrid(filenames);
        }

        private Drawable createThumbnailGrid(string[] filenames)
        {
            var rows = filenames
                       .Chunk(grid_columns)
                       .Select(chunk => createThumbnailRow(chunk))
                       .ToArray();

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(8),
                Children = rows,
            };
        }

        private Drawable createThumbnailRow(IEnumerable<string> filenames)
        {
            var filenameArray = filenames.ToArray();

            var cells = filenameArray
                        .Select(filename => (Drawable)new SpriteThumbnail(
                            filename,
                            armPlacement,
                            deleteAsset,
                            renameAsset))
                        .Concat(Enumerable.Range(0, grid_columns - filenameArray.Length).Select(_ => new Container()))
                        .ToArray();

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = Enumerable.Repeat(new Dimension(GridSizeMode.Absolute, cell_size), grid_columns).ToArray(),
                Content = new[] { cells },
            };
        }

        private IEnumerable<string> getImageFilenames()
        {
            string query = searchText.Value.Trim();

            return workingBeatmap.Value.BeatmapSetInfo.Files
                                 .Select(f => f.Filename)
                                 .Where(f => SupportedExtensions.IMAGE_EXTENSIONS.Contains(Path.GetExtension(f).ToLowerInvariant()))
                                 .Where(f => string.IsNullOrEmpty(query) || f.Contains(query, StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
        }

        private void placeSprite(string path, Vector2? textureSize = null)
        {
            state.PlacementPath.Value = null;
            state.PlacementTextureSize.Value = null;
            state.SelectedSprite.Value = DesignStoryboardOperations.PlaceSprite(beatmap, path, clock.CurrentTimeAccurate, textureSize: textureSize);
        }

        private void armPlacement(string path, Vector2? textureSize)
        {
            if (state.PlacementPath.Value == path)
            {
                state.PlacementPath.Value = null;
                state.PlacementTextureSize.Value = null;
            }
            else
            {
                state.PlacementPath.Value = path;
                state.PlacementTextureSize.Value = textureSize;
            }

            closeLibrary();
        }

        private void deleteAsset(string path)
        {
            if (state.PlacementPath.Value == path)
            {
                state.PlacementPath.Value = null;
                state.PlacementTextureSize.Value = null;
            }

            if (state.SelectedSprite.Value?.Path == path)
                state.SelectedSprite.Value = null;

            DesignStoryboardOperations.DeleteAsset(beatmap, beatmaps, workingBeatmap.Value.BeatmapSetInfo, path);
            Schedule(refreshThumbnails);
        }

        private void renameAsset(string oldPath, string newPath)
        {
            if (!DesignStoryboardOperations.RenameAsset(beatmap, beatmaps, workingBeatmap.Value, workingBeatmap.Value.BeatmapSetInfo, oldPath, newPath))
                return;

            if (state.PlacementPath.Value == oldPath)
                state.PlacementPath.Value = newPath;

            if (state.SelectedSprite.Value?.Path == oldPath)
                state.SelectedSprite.Value = null;

            Schedule(refreshThumbnails);
        }

        private void closeLibrary() => state.DialogHost?.HideDialog();

        private partial class SpriteThumbnail : CompositeDrawable, IHasContextMenu, IHasPopover
        {
            private readonly string path;
            private readonly Action<string, Vector2?> onPlacementRequested;
            private readonly Action<string> onDelete;
            private readonly Action<string, string> onRename;

            private Sprite sprite = null!;
            private Box placementHighlight = null!;
            private TextureStore? textureStore;

            public SpriteThumbnail(
                string path,
                Action<string, Vector2?> onPlacementRequested,
                Action<string> onDelete,
                Action<string, string> onRename)
            {
                this.path = path;
                this.onPlacementRequested = onPlacementRequested;
                this.onDelete = onDelete;
                this.onRename = onRename;

                Size = new Vector2(cell_size);
                Masking = true;
                CornerRadius = 5;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider, RealmAccess realmAccess, GameHost gameHost, EditorBeatmap beatmap, DesignStoryboardState state)
            {
                string? storagePath = beatmap.BeatmapInfo.BeatmapSet?.GetPathForFile(path);

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(6),
                        Spacing = new Vector2(0, 4),
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 52,
                                Masking = true,
                                CornerRadius = 4,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background3,
                                    },
                                    placementHighlight = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Highlight1,
                                        Alpha = 0,
                                    },
                                    sprite = new Sprite
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        RelativeSizeAxes = Axes.Both,
                                        FillMode = FillMode.Fit,
                                    },
                                },
                            },
                            new TruncatingSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                RelativeSizeAxes = Axes.X,
                                Text = Path.GetFileName(path),
                                Font = OsuFont.Default.With(size: 12),
                            },
                            new RoundedButton
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 24,
                                Text = @"Place",
                                Action = () => onPlacementRequested(path, getTextureSize()),
                            },
                        },
                    },
                };

                state.PlacementPath.BindValueChanged(placement =>
                {
                    placementHighlight.Alpha = placement.NewValue == path ? 0.35f : 0;
                }, true);

                if (storagePath != null)
                {
                    textureStore = new TextureStore(gameHost.Renderer, gameHost.CreateTextureLoaderStore(new RealmFileStore(realmAccess, gameHost.Storage).Store));
                    sprite.Texture = textureStore.Get(storagePath);
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                textureStore?.Dispose();
            }

            public MenuItem[] ContextMenuItems => new MenuItem[]
            {
                new OsuMenuItem(@"Place", MenuItemType.Standard, () => onPlacementRequested(path, getTextureSize())),
                new OsuMenuItem(CommonStrings.Rename, MenuItemType.Standard, this.ShowPopover),
                new OsuMenuItem(@"Delete", MenuItemType.Destructive, () => onDelete(path)),
            };

            public Popover GetPopover() => new SpriteAssetRenamePopover(path, newPath => onRename(path, newPath));

            private Vector2? getTextureSize()
            {
                if (sprite.Texture == null)
                    return null;

                return new Vector2(sprite.Texture.Width, sprite.Texture.Height);
            }

            protected override bool OnMouseDown(MouseDownEvent e) => e.Button == MouseButton.Right;
        }
    }
}
