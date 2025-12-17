using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using DerailValleyModToolbar;
using DerailValleyBindingHelper;

namespace DerailValleyCone;

#if DEBUG
[EnableReloading]
#endif
public static class Main
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        Harmony? harmony = null;
        try
        {
            settings = Settings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModToolbarAPI
                .Register(modEntry)
                .AddPanelControl(
                    label: "Cones",
                    icon: "icon.png",
                    tooltip: "Configure Cones",
                    type: typeof(ConePanel),
                    title: "Cones",
                    width: 400)
                .Finish();

            ModEntry.Logger.Log("DerailValleyCone started");
        }
        catch (Exception ex)
        {
            ModEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        modEntry.OnUnload = Unload;
        return true;
    }

    static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Draw(modEntry);
    }

    static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Save(modEntry);
    }

    private static bool Unload(UnityModManager.ModEntry modEntry)
    {
        ModToolbarAPI.Unregister(modEntry);

        ConeHelper.Unload();

        ModEntry.Logger.Log("DerailValleyCone stopped");
        return true;
    }
}
