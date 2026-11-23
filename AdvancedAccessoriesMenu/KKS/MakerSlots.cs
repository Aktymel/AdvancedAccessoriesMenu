using ChaCustom;
using KKAPI.Maker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedAccessoriesMenu
{
    internal static class MakerSlots
    {
        private const int AccessoryMenuIndex = 4;
        private const int EmptyType = 120;

        private static CustomChangeMainMenu _mainMenu;
        private static CustomAcsChangeSlot _changeSlot;

        public static void Reset()
        {
            _mainMenu = null;
            _changeSlot = null;
        }

        public static string Name(int slot)
        {
            var toggle = GetToggle(slot);
            var text = toggle == null ? null : toggle.GetComponentInChildren<TextMeshProUGUI>();
            return text == null ? null : text.text;
        }

        // Read from the slot data rather than the spawned object, which may not exist yet right after a move
        public static bool IsEmpty(int slot)
        {
#pragma warning disable 612 // GetPartsInfo is marked obsolete but is the only way that covers MoreAccessories slots
            var part = AccessoriesApi.GetPartsInfo(slot);
#pragma warning restore 612
            return part == null || part.type == EmptyType;
        }

        public static int SlotCount(int outfit)
        {
            return MakerAPI.GetCharacterControl().chaFile.coordinate[outfit].accessory.parts.Length;
        }

        public static bool IsEmpty(int outfit, int slot)
        {
            var parts = MakerAPI.GetCharacterControl().chaFile.coordinate[outfit].accessory.parts;
            return slot >= parts.Length || parts[slot].type == EmptyType;
        }

        public static void Select(int slot)
        {
            var toggle = GetToggle(slot);
            if (toggle == null) return;
            OpenAccessoryMenu();
            toggle.isOn = true;
        }

        private static void OpenAccessoryMenu()
        {
            if (_mainMenu == null) _mainMenu = Object.FindObjectOfType<CustomChangeMainMenu>();
            if (_mainMenu == null || _mainMenu.items == null || _mainMenu.items.Length <= AccessoryMenuIndex) return;
            var toggle = _mainMenu.items[AccessoryMenuIndex].tglItem;
            if (toggle != null && !toggle.isOn) toggle.isOn = true;
        }

        private static Toggle GetToggle(int slot)
        {
            if (slot < 0 || slot >= AccessoriesApi.GetMakerAccessoryCount()) return null;
            if (_changeSlot == null) _changeSlot = Object.FindObjectOfType<CustomAcsChangeSlot>();
            if (_changeSlot == null || _changeSlot.items == null || slot >= _changeSlot.items.Length) return null;
            var item = _changeSlot.items[slot];
            return item == null ? null : item.tglItem;
        }
    }
}
