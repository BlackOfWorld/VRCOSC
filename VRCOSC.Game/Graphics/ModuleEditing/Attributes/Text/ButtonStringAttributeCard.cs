﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using VRCOSC.Game.Graphics.Themes;
using VRCOSC.Game.Graphics.UI.Button;
using VRCOSC.Game.Graphics.UI.Text;
using VRCOSC.Game.Modules;

namespace VRCOSC.Game.Graphics.ModuleEditing.Attributes.Text;

public sealed partial class ButtonStringAttributeCard : TextAttributeCard<StringTextBox, string>
{
    private readonly ModuleAttributeSingleWithButton attributeSingleWithButton;

    public ButtonStringAttributeCard(ModuleAttributeSingleWithButton attributeData)
        : base(attributeData)
    {
        attributeSingleWithButton = attributeData;
    }

    protected override Drawable CreateContent()
    {
        return new GridContainer
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            RelativeSizeAxes = Axes.X,
            Height = 40,
            ColumnDimensions = new[]
            {
                new Dimension(GridSizeMode.Relative, 0.75f),
                new Dimension(GridSizeMode.Absolute, 5),
                new Dimension()
            },
            Content = new[]
            {
                new[]
                {
                    base.CreateContent(),
                    null,
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(5),
                        Child = new TextButton
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Text = attributeSingleWithButton.ButtonText,
                            Masking = true,
                            CornerRadius = 5,
                            Action = attributeSingleWithButton.ButtonAction,
                            BackgroundColour = ThemeManager.Current[ThemeAttribute.Action]
                        }
                    }
                }
            }
        };
    }
}
