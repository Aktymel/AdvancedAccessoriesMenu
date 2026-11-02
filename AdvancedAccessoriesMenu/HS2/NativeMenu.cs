using System;
using CharaCustom;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedAccessoriesMenu
{
    // The game's slot list is hidden as a whole, together with its "Copy" entry and MoreAccessories' "+1" / "+10"
    internal static class NativeMenu
    {
        private const string SelectWindowPath = "CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuAccessory";
        private const string AddButtonsPath = "Viewport/Content/Category/CategoryTop/AddButtons/";
        private const float ButtonWidth = 110;
        private const float ButtonHeight = 32;
        private const float ButtonGap = 8;

        private static CvsSelectWindow _selectWindow;
        private static RectTransform _list;
        private static CanvasGroup _listGroup;
        private static bool _hidden;
        private static Rect _buttonRect;

        public static bool HidesCopy => true;

        // Raised when the slot list is shown, hidden or resized
        public static event Action Changed;

        public static bool Shown { get; private set; }
        public static Rect ListRect { get; private set; }
        public static bool CanAddSlots { get; private set; }

        // When the maker has loaded
        public static void Attach()
        {
            var go = GameObject.Find(SelectWindowPath);
            _selectWindow = go == null ? null : go.GetComponent<CvsSelectWindow>();
            _list = _selectWindow == null ? null : _selectWindow.transform.Find("Scroll View") as RectTransform;
            if (_selectWindow != null) UiWatcher.Watch(_selectWindow.gameObject, Refresh);
            Refresh();
        }

        public static void Reset()
        {
            SetListHidden(false);
            _selectWindow = null;
            _list = null;
            _listGroup = null;
            _hidden = false;
            Shown = false;
        }

        private static void Refresh()
        {
            Shown = ScreenUi.IsShown(_selectWindow) && _list != null;
            ListRect = _list == null ? new Rect() : ScreenUi.ScreenRect(_list);
            CanAddSlots = AddButton("AddOne") != null;

            // Above the right end of the slot list, next to the "Accessories" title
            var list = ListRect;
            var scale = ScreenUi.Scale;
            var size = new Vector2(ButtonWidth * scale, ButtonHeight * scale);
            _buttonRect = new Rect(list.xMax - size.x, list.y - size.y - ButtonGap * scale, size.x, size.y);
            Changed?.Invoke();
        }

        public static bool DrawToggleButton(bool advancedOpen)
        {
            return ScreenUi.DrawToggleButton(_buttonRect, advancedOpen);
        }

        public static void SetListHidden(bool hidden)
        {
            if (hidden == _hidden || _list == null) return;
            _hidden = hidden;
            if (_listGroup == null) _listGroup = ScreenUi.GetOrAddCanvasGroup(_list.gameObject);
            ScreenUi.Hide(_listGroup, hidden);
        }

        public static void OpenCopy()
        {
            if (_selectWindow == null || _selectWindow.items == null) return;
            foreach (var item in _selectWindow.items)
            {
                if (item == null || !(item.cvsBase is CvsA_Copy) || item.btnItem == null) continue;
                item.btnItem.onClick.Invoke();
                return;
            }
        }

        public static void AddSlots(bool ten)
        {
            var button = AddButton(ten ? "AddTen" : "AddOne");
            if (button != null) button.onClick.Invoke();
        }

        private static Button AddButton(string name)
        {
            if (_list == null) return null;
            var t = _list.Find(AddButtonsPath + name);
            return t == null ? null : t.GetComponent<Button>();
        }
    }
}
