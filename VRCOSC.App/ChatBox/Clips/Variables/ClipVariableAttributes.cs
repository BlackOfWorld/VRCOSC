﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;

namespace VRCOSC.App.ChatBox.Clips.Variables;

public class ClipVariableOptionAttribute : Attribute
{
    public string SerialisedName { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public ClipVariableOptionAttribute(string serialisedName, string displayName, string description)
    {
        SerialisedName = serialisedName;
        DisplayName = displayName;
        Description = description;
    }
}
