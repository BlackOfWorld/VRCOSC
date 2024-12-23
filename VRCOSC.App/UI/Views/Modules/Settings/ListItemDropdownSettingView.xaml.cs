﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using VRCOSC.App.SDK.Modules;
using VRCOSC.App.SDK.Modules.Attributes.Settings;

namespace VRCOSC.App.UI.Views.Modules.Settings;

public partial class ListItemDropdownSettingView
{
    public ListItemDropdownSettingView(Module _, DropdownListModuleSetting dropdownListModuleSetting)
    {
        InitializeComponent();

        DataContext = dropdownListModuleSetting;
    }
}