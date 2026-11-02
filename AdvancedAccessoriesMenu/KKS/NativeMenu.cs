using System;
using System.Collections.Generic;
using ChaCustom;
using KKAPI.Maker;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedAccessoriesMenu
{
    // A slot's settings page is part of its entry in the slot list, so only the slot buttons are hidden (with the
    // list's scrollbar and MoreAccessories' "+1" / "+10"); the settings pages, "Copy" and "Transfer" stay
    internal static class NativeMenu
    {
        private const string SlotButtonName = "imgOff";
        private const string ScrollViewName = "Slots";
        private const string AddButtonsPath = "Slots/Viewport/Content/Add Buttons Group/";
        private const float ButtonWidth = 110;
        private const float Gap = 6;

        private static CustomAcsChangeSlot _changeSlot;
        private static ScrollRect _scroll;
        private static readonly HashSet<CanvasGroup> HiddenGroups = new HashSet<CanvasGroup>();
        private static int _hiddenSlotCount = -1;
        private static Rect _buttonRect;
        private static bool _slotEventAdded;

        public static bool HidesCopy => false;

        // Raised when the slot list is shown, hidden, resized, scrolled or gets slots
        public static event Action Changed;

        public static bool Shown { get; private set; }
        public static Rect ListRect { get; private set; }
        public static bool CanAddSlots { get; private set; }

        // When the maker has loaded
        public static void Attach()
        {
            _changeSlot = UnityEngine.Object.FindObjectOfType<CustomAcsChangeSlot>();
            var scroll = _changeSlot == null ? null : _changeSlot.transform.Find(ScrollViewName);
            _scroll = scroll == null ? null : scroll.GetComponent<ScrollRect>();
            if (_changeSlot != null) UiWatcher.Watch(_changeSlot.gameObject, Refresh);
            if (_scroll != null) _scroll.onValueChanged.AddListener(OnScrolled);
            if (!_slotEventAdded)
            {
                _slotEventAdded = true;
                AccessoriesApi.MakerAccSlotAdded += (sender, e) => Refresh();
            }
            Refresh();
        }

        public static void Reset()
        {
            SetListHidden(false);
            if (_scroll != null) _scroll.onValueChanged.RemoveListener(OnScrolled);
            _scroll = null;
            _changeSlot = null;
            Shown = false;
        }

        private static void OnScrolled(Vector2 position) => Refresh();

        private static void Refresh()
        {
            if (_changeSlot == null)
            {
                Shown = false;
                Changed?.Invoke();
                return;
            }
            var first = SlotButton(0);
            Shown = first != null && ScreenUi.IsShown(_changeSlot);
            CanAddSlots = AddButton(false) != null;
            ListRect = first == null ? new Rect() : FindListRect(first);
            _buttonRect = FindButtonRect();
            Changed?.Invoke();
        }

        private static Rect FindListRect(RectTransform first)
        {
            var firstRect = ScreenUi.ScreenRect(first);

            // MoreAccessories puts the slots into a scroll view; without it the list ends at the last slot
            var scroll = _scroll == null ? null : (RectTransform)_scroll.transform;
            var last = SlotButton(AccessoriesApi.GetMakerAccessoryCount() - 1);
            var top = scroll != null ? ScreenUi.ScreenRect(scroll).y : firstRect.y;
            var bottom = scroll != null ? ScreenUi.ScreenRect(scroll).yMax : last != null ? ScreenUi.ScreenRect(last).yMax : firstRect.yMax;

            // Up to the settings page next to the slot buttons
            var right = firstRect.xMax;
            var page = _changeSlot.items[0].cgItem;
            if (page != null) right = Mathf.Max(right, ScreenUi.ScreenRect((RectTransform)page.transform).xMin - Gap * ScreenUi.Scale);

            return new Rect(firstRect.x, top, right - firstRect.x, bottom - top);
        }

        // Right of the "Copy" button
        private static Rect FindButtonRect()
        {
            var items = _changeSlot.items;
            var copy = items.Length < 2 || items[items.Length - 2].tglItem == null ? null : items[items.Length - 2].tglItem.transform.Find(SlotButtonName) as RectTransform;
            if (copy == null) return new Rect();
            var copyRect = ScreenUi.ScreenRect(copy);
            return new Rect(copyRect.xMax + Gap * ScreenUi.Scale, copyRect.y, ButtonWidth * ScreenUi.Scale, copyRect.height);
        }

        public static bool DrawToggleButton(bool advancedOpen)
        {
            return _buttonRect.width > 0 && ScreenUi.DrawToggleButton(_buttonRect, advancedOpen);
        }

        public static void SetListHidden(bool hidden)
        {
            if (!hidden)
            {
                foreach (var group in HiddenGroups)
                    if (group != null) ScreenUi.Hide(group, false);
                HiddenGroups.Clear();
                _hiddenSlotCount = -1;
                return;
            }

            // MoreAccessories can add slots while the list is hidden
            var count = AccessoriesApi.GetMakerAccessoryCount();
            if (count == _hiddenSlotCount || _changeSlot == null) return;
            _hiddenSlotCount = count;

            var items = _changeSlot.items;
            for (var slot = 0; slot < count && slot < items.Length; slot++)
            {
                var toggle = items[slot].tglItem;
                if (toggle == null) continue;
                var page = items[slot].cgItem;
                foreach (Transform child in toggle.transform)
                    if (page == null || !page.transform.IsChildOf(child)) Hide(child);
            }

            if (_scroll != null && _scroll.verticalScrollbar != null) Hide(_scroll.verticalScrollbar.transform);
            var addButtons = _changeSlot.transform.Find(AddButtonsPath.TrimEnd('/'));
            if (addButtons != null) Hide(addButtons);
        }

        public static void OpenCopy() { }

        public static void AddSlots(bool ten)
        {
            var button = AddButton(ten);
            if (button != null) button.onClick.Invoke();
        }

        private static Button AddButton(bool ten)
        {
            if (_changeSlot == null) return null;
            var t = _changeSlot.transform.Find(AddButtonsPath + (ten ? "Add Ten Button" : "Add One Button"));
            return t == null ? null : t.GetComponent<Button>();
        }

        private static void Hide(Transform t)
        {
            var group = ScreenUi.GetOrAddCanvasGroup(t.gameObject);
            ScreenUi.Hide(group, true);
            HiddenGroups.Add(group);
        }

        private static RectTransform SlotButton(int slot)
        {
            if (_changeSlot == null || _changeSlot.items == null || slot < 0 || slot >= _changeSlot.items.Length) return null;
            var toggle = _changeSlot.items[slot].tglItem;
            return toggle == null ? null : toggle.transform.Find(SlotButtonName) as RectTransform;
        }
    }
}
