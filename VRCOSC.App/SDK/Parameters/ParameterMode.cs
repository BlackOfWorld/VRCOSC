﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;

namespace VRCOSC.App.SDK.Parameters;

[Flags]
public enum ParameterMode
{
    /// <summary>
    /// Has the ability to read from VRChat
    /// </summary>
    Read = 1 << 0,

    /// <summary>
    /// Has the ability to write to VRChat
    /// </summary>
    Write = 1 << 1,

    /// <summary>
    /// Has the ability to read from and write to VRChat
    /// </summary>
    ReadWrite = Read | Write
}
