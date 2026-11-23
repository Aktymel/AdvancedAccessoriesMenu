using System;
using BepInEx;
using BepInEx.Bootstrap;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using TMPro;
using UnityEngine.UI;

namespace AdvancedAccessoriesMenu
{
    // Moves an accessory with the game's own Transfer page and then empties the source slot like choosing "None".
    // KKAPI reports both to other plugins, so Material Editor and the like move their data along.
    internal static class SlotMover
    {
        public static bool Available => true;

        // Pregnancy Plus before 7.9 throws in its patch on the Transfer page, which breaks every move and copy
        public static string Warning
        {
            get
            {
                if (!_warningChecked)
                {
                    _warningChecked = true;
                    _warning = FindWarning();
                }
                return _warning;
            }
        }

        private static bool _warningChecked;
        private static string _warning;

        private static string FindWarning()
        {
#if KK
            // Pregnancy Plus for KK has its own versions (6.x) and isn't checked yet
            return null;
#else
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue("KK_PregnancyPlus", out info)) return null;
            return info.Metadata.Version < new Version(7, 9) ? "Pregnancy Plus 7.9+ needed for Move/Copy." : null;
#endif
        }

        public static bool Move(int source, int destination)
        {
            var changeSlot = ScreenUi.FindInScene<CustomAcsChangeSlot>();
            var sourcePage = changeSlot == null ? null : SlotPage(changeSlot, source);
            if (sourcePage == null)
            {
                AdvancedAccessoriesMenuPlugin.Logger.LogWarning($"Move {source + 1} -> {destination + 1}: no slot page for {source + 1}");
                return false;
            }
            if (!Copy(source, destination)) return false;

            var shownBefore = Dropdown(sourcePage)?.value;
            Clear(sourcePage);
            if (MakerSlots.IsEmpty(source)) return true;
            AdvancedAccessoriesMenuPlugin.Logger.LogWarning($"Move {source + 1} -> {destination + 1}: slot {source + 1} isn't empty after clearing, " +
                                                            $"part {Part(source)}, dropdown {shownBefore} -> {Dropdown(sourcePage)?.value}");
            return false;
        }

        private static string Part(int slot)
        {
#pragma warning disable 612
            var part = AccessoriesApi.GetPartsInfo(slot);
#pragma warning restore 612
            return part == null ? "none" : $"{part.type}/{part.id}";
        }

        private static TMP_Dropdown Dropdown(CvsAccessory page)
        {
            return Traverse.Create(page).Field("ddAcsType").GetValue<TMP_Dropdown>();
        }

        // The destination must be empty
        public static bool Copy(int source, int destination)
        {
            var transfer = ScreenUi.FindInScene<CvsAccessoryChange>();
            if (transfer == null) return false;

            var page = Traverse.Create(transfer);
            var oldSource = page.Field("selSrc").GetValue<int>();
            var oldDestination = page.Field("selDst").GetValue<int>();
            var mirror = page.Field("tglReverse").GetValue<Toggle>();
            var mirrored = mirror != null && mirror.isOn;
            try
            {
                if (mirrored) mirror.isOn = false;
                page.Field("selSrc").SetValue(source);
                page.Field("selDst").SetValue(destination);
                AccessTools.Method(typeof(CvsAccessoryChange), "CopyAcs").Invoke(transfer, null);
            }
            finally
            {
                page.Field("selSrc").SetValue(oldSource);
                page.Field("selDst").SetValue(oldDestination);
                if (mirrored) mirror.isOn = true;
            }
            if (!MakerSlots.IsEmpty(destination)) return true;
            AdvancedAccessoriesMenuPlugin.Logger.LogWarning($"Copy {source + 1} -> {destination + 1}: slot {destination + 1} is still empty, source part {Part(source)}");
            return false;
        }

        // The names from the game's Copy window
        public static string[] OutfitNames()
        {
            var count = MakerAPI.GetCharacterControl().chaFile.coordinate.Length;
            var window = ScreenUi.FindInScene<CvsAccessoryCopy>();
            var dropdowns = window == null ? null : Traverse.Create(window).Field("ddCoordeType").GetValue<TMP_Dropdown[]>();
            var options = dropdowns == null || dropdowns.Length == 0 ? null : dropdowns[0].options;
            var names = new string[count];
            for (var i = 0; i < count; i++)
                names[i] = options != null && i < options.Count ? options[i].text : "Outfit " + (i + 1);
            return names;
        }

        // Into the same slots of another outfit with the game's Copy window, so KKAPI raises AccessoriesCopied
        public static string CopyToOutfit(int first, int last, int outfit)
        {
            var window = ScreenUi.FindInScene<CvsAccessoryCopy>();
            var page = window == null ? null : Traverse.Create(window);
            var dropdowns = page == null ? null : page.Field("ddCoordeType").GetValue<TMP_Dropdown[]>();
            var toggles = page == null ? null : page.Field("tglKind").GetValue<Toggle[]>();
            if (dropdowns == null || dropdowns.Length < 2 || toggles == null) return "The game's Copy window is missing.";
            Diag.Log($"Copy {first + 1}-{last + 1} to outfit {outfit}, {toggles.Length} copy toggles, window active {window.gameObject.activeInHierarchy}");
            if (toggles.Length <= last) return "Open the game's Copy window once, then try again.";

            var current = MakerAPI.GetCharacterControl().fileStatus.coordinateType;
            var oldDestination = dropdowns[0].value;
            var oldSource = dropdowns[1].value;
            var oldToggles = new bool[toggles.Length];
            for (var i = 0; i < toggles.Length; i++) oldToggles[i] = toggles[i].isOn;
            try
            {
                dropdowns[0].value = outfit;
                dropdowns[1].value = current;
                for (var i = 0; i < toggles.Length; i++) toggles[i].isOn = i >= first && i <= last && !MakerSlots.IsEmpty(i);
                AccessTools.Method(typeof(CvsAccessoryCopy), "CopyAcs").Invoke(window, null);
            }
            catch (Exception e)
            {
                AdvancedAccessoriesMenuPlugin.Logger.LogWarning($"Copy {first + 1}-{last + 1} to outfit {outfit}: {e}");
                return "Copying failed.";
            }
            finally
            {
                for (var i = 0; i < toggles.Length; i++) toggles[i].isOn = oldToggles[i];
                dropdowns[0].value = oldDestination;
                dropdowns[1].value = oldSource;
            }

            for (var slot = first; slot <= last; slot++)
            {
                if (MakerSlots.IsEmpty(slot) || !MakerSlots.IsEmpty(outfit, slot)) continue;
                AdvancedAccessoriesMenuPlugin.Logger.LogWarning($"Copy to outfit {outfit}: slot {slot + 1} is still empty there, source part {Part(slot)}");
                return "Copying failed.";
            }
            return null;
        }

        // Picks "None" in the slot page's type dropdown like the player does. The handler must run while the dropdown
        // shows "None", or it puts the kind window's accessory back into the slot.
        private static void Clear(CvsAccessory page)
        {
            var dropdown = Dropdown(page);
            if (dropdown == null) return;
            if (dropdown.value != 0)
            {
                dropdown.value = 0;
                return;
            }
            // Already "None", so setting it wouldn't call the handler
            page.UpdateSelectAccessoryType(0);
            page.ChangeSettingVisible(false);
        }

        private static CvsAccessory SlotPage(CustomAcsChangeSlot changeSlot, int slot)
        {
            foreach (var page in changeSlot.GetComponentsInChildren<CvsAccessory>(true))
                if ((int)page.slotNo == slot) return page;
            return null;
        }
    }
}
