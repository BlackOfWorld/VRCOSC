﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.SDK.Handlers;

public interface IVRCClientEventHandler
{
    public void OnWorldExit();
    public void OnWorldEnter(string worldID);
}
