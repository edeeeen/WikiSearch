using System;
using System.Diagnostics;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Runtime.InteropServices;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using SamplePlugin.Windows;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/wiki";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Wiki Search");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    private IDataManager DM { get; init; }
    public IContextMenu ContextMenu { get; init; }

    public Plugin(IDataManager dataManager, IContextMenu contextMenu)
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Type your search after and it will open a wiki page in your browser searching for it"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [WikiSearch] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
        
        this.ContextMenu = contextMenu;
        this.ContextMenu.OnMenuOpened += this.OnContextMenuOpened;
        this.DM = dataManager;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        
        this.ContextMenu.OnMenuOpened -= this.OnContextMenuOpened;

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        OpenUrl("https://ffxiv.consolegameswiki.com/mediawiki/index.php?search=" + args.Replace(" ", "+"));
    }
    
    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        uint itemId;

        if (args.MenuType == ContextMenuType.Inventory)
        {
            itemId = (args.Target as MenuTargetInventory)?.TargetItem?.ItemId ?? 0u;
        }
        else
        {
            itemId = this.GetItemIdFromAgent(args.AddonName);
            
        }
        if (itemId == 0u)
        {
            //Log.Warning("Failed to get item ID");
            return;
        }
        
        var item = this.DM.Excel.GetSheet<Item>().GetRowOrDefault(itemId);

        args.AddMenuItem(new MenuItem
        {
            Name = "Search Wiki",
            OnClicked = this.ContextMenuOpenUrl(itemId),
            Prefix = SeIconChar.BoxedLetterW,
            PrefixColor = 12,
        });
    }

    private Action<IMenuItemClickedArgs> ContextMenuOpenUrl(uint itemid)
    {
        return (IMenuItemClickedArgs args) =>
        {
            OpenUrl("https://ffxiv.consolegameswiki.com/mediawiki/index.php?search=id-gt+%3D+" + itemid);

        };
    }
    private unsafe uint GetItemIdFromAgent(string? addonName)
    {
        var itemId = addonName switch
        {
            "ChatLog" => AgentChatLog.Instance()->ContextItemId,
            "GatheringNote" => *(uint*)((IntPtr)AgentGatheringNote.Instance() + 0xA0),
            "GrandCompanySupplyList" => *(uint*)((IntPtr)AgentGrandCompanySupply.Instance() + 0x54),
            "ItemSearch" => (uint)AgentContext.Instance()->UpdateCheckerParam,
            "RecipeNote" => AgentRecipeNote.Instance()->ContextMenuResultItemId,
            _ => 0u,
        };

        return itemId % 500000;
    }
    
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
