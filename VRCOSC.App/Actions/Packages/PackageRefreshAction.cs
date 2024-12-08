﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VRCOSC.App.Packages;
using VRCOSC.App.Utils;

namespace VRCOSC.App.Actions.Packages;

public class PackagesRefreshAction : CompositeProgressAction
{
    private readonly ObservableCollection<PackageSource> sources;
    private readonly bool forceRemoteGrab;

    public PackagesRefreshAction(ObservableCollection<PackageSource> sources, bool forceRemoteGrab)
    {
        this.sources = sources;
        this.forceRemoteGrab = forceRemoteGrab;
    }

    protected override async Task Perform()
    {
        sources.ForEach(source => AddAction(new PackageSourceRefreshAction(source, forceRemoteGrab)));
        await base.Perform();
    }
}

public class PackageSourceRefreshAction : ProgressAction
{
    private readonly PackageSource source;
    private readonly string packageSourceDisplayName;
    private readonly bool forceRemoteGrab;

    public override string Title => $"Refreshing {packageSourceDisplayName}";

    public PackageSourceRefreshAction(PackageSource source, bool forceRemoteGrab)
    {
        this.source = source;
        packageSourceDisplayName = source.DisplayName;
        this.forceRemoteGrab = forceRemoteGrab;
    }

    protected override Task Perform()
    {
        return source.Refresh(forceRemoteGrab);
    }

    public override float GetProgress() => 0f;
}
