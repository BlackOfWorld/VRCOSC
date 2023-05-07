﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using VRCOSC.Game.Graphics.Themes;
using VRCOSC.Game.Modules;
using VRCOSC.Game.OSC.VRChat;

namespace VRCOSC.Game.Graphics.Run;

public sealed partial class ParameterContainer : Container
{
    [Resolved]
    private GameManager gameManager { get; set; } = null!;

    private readonly ParameterSubContainer outgoingParameterDisplay;
    private readonly ParameterSubContainer incomingParameterDisplay;

    public ParameterContainer()
    {
        Child = new GridContainer
        {
            RelativeSizeAxes = Axes.Both,
            RowDimensions = new[]
            {
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, 5),
                new Dimension()
            },
            Content = new[]
            {
                new Drawable[]
                {
                    outgoingParameterDisplay = new ParameterSubContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        BorderThickness = 3,
                        Masking = true,
                        CornerRadius = 10,
                        Title = "Outgoing"
                    }
                },
                null,
                new Drawable[]
                {
                    incomingParameterDisplay = new ParameterSubContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        BorderThickness = 3,
                        Masking = true,
                        CornerRadius = 10,
                        Title = "Incoming"
                    }
                }
            }
        };
    }

    protected override void LoadComplete()
    {
        gameManager.VRChatOscClient.OnParameterSent += onParameterSent;
        gameManager.VRChatOscClient.OnParameterReceived += onParameterReceived;

        gameManager.State.BindValueChanged(e =>
        {
            if (e.NewValue == GameManagerState.Starting) ClearParameters();
        });
    }

    private void onParameterSent(VRChatOscData data)
    {
        outgoingParameterDisplay.AddEntry(data.Address, data.ParameterValue);
    }

    private void onParameterReceived(VRChatOscData data)
    {
        incomingParameterDisplay.AddEntry(data.Address, data.ParameterValue);
    }

    public void ClearParameters() => Schedule(() =>
    {
        outgoingParameterDisplay.ClearContent();
        incomingParameterDisplay.ClearContent();
    });

    private sealed partial class ParameterSubContainer : Container
    {
        private ParameterDisplay parameterDisplay = null!;

        public string Title { get; init; } = string.Empty;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ThemeManager.Current[ThemeAttribute.Darker]
                },
                parameterDisplay = new ParameterDisplay
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding
                    {
                        Vertical = 1.5f,
                        Horizontal = 3
                    },
                    Title = Title
                }
            };
        }

        public void AddEntry(string key, object value)
            => parameterDisplay.AddEntry(key, value);

        public void ClearContent()
            => parameterDisplay.ClearContent();
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);
        gameManager.VRChatOscClient.OnParameterSent -= onParameterSent;
        gameManager.VRChatOscClient.OnParameterReceived -= onParameterReceived;
    }
}
