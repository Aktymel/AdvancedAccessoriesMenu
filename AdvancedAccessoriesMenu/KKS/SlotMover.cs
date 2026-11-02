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
