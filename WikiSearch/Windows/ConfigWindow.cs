using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("WikiSearch")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 60);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        
    }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = Configuration.ContextMenu;
        if (ImGui.Checkbox("Context Menu", ref configValue))
        {
            Configuration.ContextMenu = configValue;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }
    }
}
