﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using VRCOSC.App.ChatBox.Clips.Variables;

namespace VRCOSC.App.UI.Views.ChatBox.Variables;

public partial class TextBoxVariableOptionView
{
    public TextBoxVariableOptionView(ClipVariable instance, PropertyInfo propertyInfo)
    {
        InitializeComponent();

        var textBoxBinding = new Binding(propertyInfo.Name)
        {
            Source = instance
        };

        ValueTextBox.SetBinding(TextBox.TextProperty, textBoxBinding);
    }
}