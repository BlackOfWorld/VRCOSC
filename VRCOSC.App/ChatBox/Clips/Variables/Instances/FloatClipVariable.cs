﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Globalization;

namespace VRCOSC.App.ChatBox.Clips.Variables.Instances;

public class FloatClipVariable : ClipVariable
{
    public FloatClipVariable()
    {
    }

    public FloatClipVariable(ClipVariableReference reference)
        : base(reference)
    {
    }

    [ClipVariableOption("float_format", "Float Format", "How should the float be formatted?")]
    public string FloatFormat { get; set; } = "F1";

    public override bool IsDefault() => base.IsDefault() && FloatFormat == "F1";

    public override FloatClipVariable Clone()
    {
        var clone = (FloatClipVariable)base.Clone();

        clone.FloatFormat = FloatFormat;

        return clone;
    }

    protected override string Format(object value)
    {
        try
        {
            return ((float)value).ToString(FloatFormat, CultureInfo.CurrentCulture);
        }
        catch (Exception)
        {
            return "INVALID FORMAT";
        }
    }
}
