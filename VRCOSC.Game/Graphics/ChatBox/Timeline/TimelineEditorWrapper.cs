﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace VRCOSC.Game.Graphics.ChatBox.Timeline;

public partial class TimelineEditorWrapper : Container
{
    [BackgroundDependencyLoader]
    private void load()
    {
        Child = new GridContainer
        {
            RelativeSizeAxes = Axes.Both,
            ColumnDimensions = new[]
            {
                new Dimension(GridSizeMode.Relative, 0.175f),
                new Dimension(GridSizeMode.Absolute, 5),
                new Dimension(),
            },
            Content = new[]
            {
                new Drawable?[]
                {
                    new TimelineMetadataEditor
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 10
                    },
                    null,
                    new TimelineEditor
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 10
                    }
                }
            }
        };
    }
}
