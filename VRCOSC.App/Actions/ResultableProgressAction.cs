﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.Actions;

public abstract class ResultableProgressAction<T> : ProgressAction
{
    public T? Result { get; protected set; }
}
