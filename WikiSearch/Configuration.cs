using Dalamud.Configuration;
using System;

namespace WikiSearch;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    //public bool IsConfigWindowMovable { get; set; } = true;
    public bool ContextMenu { get; set; } = true;

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
