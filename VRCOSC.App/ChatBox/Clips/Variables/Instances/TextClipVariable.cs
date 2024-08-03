﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.ChatBox.Clips.Variables.Instances;

internal class TextClipVariable : ClipVariable
{
    public override bool IsDefault() => base.IsDefault() && Text == string.Empty;

    [ClipVariableOption("text", "Text", "The text to display")]
    public string Text { get; set; } = string.Empty;

    public TextClipVariable(ClipVariableReference reference)
        : base(reference)
    {
    }

    protected override string Format(object value) => Text;
}
