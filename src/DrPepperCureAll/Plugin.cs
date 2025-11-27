using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Items;
using UnityEngine;
using UnityEngine.Audio;

namespace DrPepperCureAll;

[BepInAutoPlugin]
[BepInDependency("com.github.PEAKModding.PEAKLib.Core", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.github.PEAKModding.PEAKLib.Items", BepInDependency.DependencyFlags.HardDependency)]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static ModDefinition definition = null!;
    internal static AssetBundle drpepperAssetBundle = null!;
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
        definition = ModDefinition.GetOrCreate(Info.Metadata);
        Harmony.CreateAndPatchAll(typeof(Patcher));

        configDrPepperShatter = Config.Bind("Toggles", "ShatterToggle", false, "Enables shattering when thrown (like the original Cure-All) if true, disables if false");

        string drpepperAssetBundlePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "drpeppercureall");
        drpepperAssetBundle = AssetBundle.LoadFromFile(drpepperAssetBundlePath);
        drpepperPrefab = drpepperAssetBundle.LoadAsset<GameObject>("DrPepper.prefab");
        drpepperItem = drpepperPrefab.GetComponent<Item>();
        
        LocalizedText.mainTable["NAME_DR. PEPPER"] = ["DR. PEPPER"]; // fixes the localization mumbo jumbo
        new ItemContent(drpepperItem).Register(definition);

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
