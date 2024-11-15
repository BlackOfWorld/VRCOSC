﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.ChatBox.Clips.Variables.Instances;

public class BoolClipVariable : ClipVariable
{
    public BoolClipVariable()
    {
    }

    public BoolClipVariable(ClipVariableReference reference)
        : base(reference)
    {
    }

    [ClipVariableOption("when_true", "When True", "What string should be used when this variable is true?")]
    public string WhenTrue { get; set; } = "True";

    [ClipVariableOption("when_false", "When False", "What string should be used when this variable is false?")]
    public string WhenFalse { get; set; } = "False";

    public override bool IsDefault() => base.IsDefault() && WhenTrue == "True" && WhenFalse == "False";

    public override BoolClipVariable Clone()
    {
        var clone = (BoolClipVariable)base.Clone();

        clone.WhenTrue = WhenTrue;
        clone.WhenFalse = WhenFalse;

        return clone;
    }

    protected override string Format(object value) => (bool)value ? WhenTrue : WhenFalse;
}