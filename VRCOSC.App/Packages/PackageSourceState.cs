﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.Packages;

public enum PackageSourceState
{
    Unknown,
    MissingRepo,
    NoReleases,
    InvalidPackageFile,
    Valid
}
