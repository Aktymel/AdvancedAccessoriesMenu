using System;
using CharaCustom;
using HarmonyLib;

namespace AdvancedAccessoriesMenu
{
    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(CustomBase), nameof(CustomBase.ChangeAcsSlotName))]
        private static void ChangeAcsSlotNamePostfix()
        {
            AccessoryEvents.Changed();
        }
#if HS2

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneSpriteAccessoryCondition), nameof(HSceneSpriteAccessoryCondition.Init))]
        private static void AccessoryPanelInitPostfix(HSceneSpriteAccessoryCondition __instance)
        {
            if (HSceneWindow.Instance != null) HSceneWindow.Instance.SetPanel(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneSpriteAccessoryCondition), nameof(HSceneSpriteAccessoryCondition.SetAccessoryCharacter))]
        private static void SetAccessoryCharacterPostfix(HSceneSpriteAccessoryCondition __instance)
        {
            if (HSceneWindow.Instance != null) HSceneWindow.Instance.CharacterChanged();
        }
#endif

        public static void PatchMoreAccessories(Harmony harmony)
        {
            var type = AccessTools.TypeByName("MoreAccessoriesAI.MoreAccessories");
            if (type == null) return;
            var postfix = new HarmonyMethod(typeof(Hooks), nameof(AddSlotsPostfix));
            foreach (var name in new[] { "AddOneSlot", "AddTenSlots" })
            {
                var method = AccessTools.Method(type, name);
                if (method != null) harmony.Patch(method, postfix: postfix);
            }
        }

        // MoreAccessories shows slot buttons kept from the previous character again without renaming them,
        // so they still show that character's accessories
        private static void AddSlotsPostfix()
        {
            // Must never break MoreAccessories' own "+1" / "+10"
            try
            {
                var customBase = Singleton<CustomBase>.Instance;
                if (customBase != null) customBase.ChangeAcsSlotName();
            }
            catch (Exception e)
            {
                AdvancedAccessoriesMenuPlugin.Logger.LogWarning("Could not update slot names: " + e);
            }
        }
    }
}
