﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace VRCOSC.Game.Modules;

public class ModuleData
{
    [JsonProperty("settings")]
    public Dictionary<string, object> Settings { get; } = new();

    [JsonProperty("parameters")]
    public Dictionary<string, string> Parameters { get; } = new();
}