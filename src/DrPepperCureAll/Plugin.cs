using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Items;
using PEAKLib.Items.UnityEditor;
using UnityEngine;

namespace DrPepperCureAll;

[BepInAutoPlugin]
[BepInDependency(ItemsPlugin.Id)]
[BepInDependency(CorePlugin.Id)]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static ModDefinition definition = null!;
    internal static GameObject drpepperPrefab = null!;
    internal static Item drpepperItem = null!;
    private ConfigEntry<bool> configDrPepperShatter = null!;

    private class Patcher
    {
        // whenever a cure-all is spawned, replace it (for general spawns like statues)
        [HarmonyPatch(typeof(Item), "Awake")]
        [HarmonyPostfix]
        private static void ItemAwakePostfix(Item __instance)
        {
            if (__instance.itemID == 24)
            {
                Transform itemTransform = __instance.transform;
                GameObject drpepperInstance = Instantiate(drpepperPrefab);
                drpepperInstance.transform.SetPositionAndRotation(itemTransform.position, itemTransform.rotation);
                Destroy(itemTransform);
            }
        }

        // whenever a cure-all is grabbed from database, give dr. pepper instead (for manual item spawning)
        [HarmonyPatch(typeof(ItemDatabase), "TryGetItem")]
        [HarmonyPostfix]
        private static void ItemDatabaseTryGetItemPostfix(ushort itemID, ref Item item, ref bool __result)
        {
            if (itemID == 24)
            {
                item = drpepperItem;
                __result = true;
            }
        }
    }

    private void Awake()
    {
        Log = Logger;
        Harmony.CreateAndPatchAll(typeof(Patcher));

        configDrPepperShatter = Config.Bind("Toggles", "ShatterToggle", false, "Enables shattering when thrown (like the original Cure-All) if true, disables if false");

        this.LoadBundleAndContentsWithName(
            "drpepper.peakbundle",
            peakBundle =>
            {
                var drpepperContent = peakBundle.LoadAsset<UnityItemContent>("DrPepperContent");
                drpepperPrefab = drpepperContent.ItemPrefab;
                drpepperPrefab.GetComponent<Breakable>().breakOnCollision = configDrPepperShatter.Value;
                drpepperItem = drpepperPrefab.GetComponent<Item>();
                peakBundle.Mod.RegisterContent();
            }
        );

        LocalizedText.mainTable["NAME_DR PEPPER"] = ["DR PEPPER"]; // fixes the localization mumbo jumbo

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
