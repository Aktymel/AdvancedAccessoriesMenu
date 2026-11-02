using System;
using System.Collections.Generic;
using ChaCustom;
using HarmonyLib;
using Object = UnityEngine.Object;

namespace AdvancedAccessoriesMenu
{
    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSlotName))]
        private static void UpdateSlotNamePostfix()
        {
            AccessoryEvents.Changed();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSprite), nameof(HSprite.Init))]
        private static void HSpriteInitPostfix(HSprite __instance, List<ChaControl> _female)
        {
            if (HSceneWindow.Instance != null) HSceneWindow.Instance.SetSprite(__instance, _female);
        }

        public static void PatchMoreAccessories(Harmony harmony)
        {
            var type = AccessTools.TypeByName("MoreAccessoriesKOI.Accessories");
            var method = type == null ? null : AccessTools.Method(type, "AddSlot");
            if (method != null) harmony.Patch(method, postfix: new HarmonyMethod(typeof(Hooks), nameof(AddSlotPostfix)));
        }

        // MoreAccessories shows slot buttons kept from the previous character again without renaming them,
        // so they still show that character's accessories
        private static void AddSlotPostfix()
        {
            // Must never break MoreAccessories' own "+1" / "+10"
            try
            {
                var changeSlot = Object.FindObjectOfType<CustomAcsChangeSlot>();
                if (changeSlot == null) return;
                foreach (var cvs in changeSlot.GetComponentsInChildren<CvsAccessory>(true))
                    cvs.UpdateSlotName();
            }
            catch (Exception e)
            {
                AdvancedAccessoriesMenuPlugin.Logger.LogWarning("Could not update slot names: " + e);
            }
        }
    }
}
