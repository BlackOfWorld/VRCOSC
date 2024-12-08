﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

// ReSharper disable MemberCanBePrivate.Global

namespace VRCOSC.App.OSC.VRChat;

public static class VRChatOscConstants
{
    public const string ADDRESS_AVATAR_PARAMETERS_PREFIX = "/avatar/parameters/";
    public const string ADDRESS_AVATAR_CHANGE = "/avatar/change";
    public const string ADDRESS_CHATBOX_INPUT = "/chatbox/input";
    public const string ADDRESS_CHATBOX_TYPING = "/chatbox/typing";

    public const double UPDATE_FREQUENCY_SECONDS = 20;
    public const double UPDATE_DELTA_MILLISECONDS = 1000d / UPDATE_FREQUENCY_SECONDS;
}
