using System;
using System.Collections.Generic;
using KKAPI.Maker;
using UnityEngine;
#if HS2 || AI
using AIChara;
#endif

namespace AdvancedAccessoriesMenu
{
    // Read-only list of a character's groups with Hide / Show, for Studio and H scenes. Groups are edited only in the maker.
    internal sealed class GroupsListView
    {
        private const float RowHeight = 24;
        private const float ButtonHeight = 22;

        // A group header (Slot is -1) or a slot; Group is null for "Ungrouped". Shown is null when there's no accessory.
        private struct Row
        {
            public SlotGroup Group;
            public int Slot;
            public string Text;
            public bool? Shown;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Vector2 _scroll;
        private float _viewHeight = 400;
        private int _firstRow;
        private int _endRow;
        private bool _ungroupedCollapsed;
        private ChaControl _builtFor;
        private int _builtVersion = -1;
        private GroupsController _controller;

        private GUIStyle _rowButtonStyle;
        private GUIStyle _groupStyle;
        private GUIStyle _slotStyle;

        // Call inside a window drawn with IMGUIUtils.SolidBackgroundGuiSkin
        public void Draw(ChaControl chaCtrl, Action<int, bool> setShown, Action changed)
        {
            if (chaCtrl != _builtFor)
            {
                _builtFor = chaCtrl;
                _controller = GroupsController.Get(chaCtrl);
                _builtVersion = -1;
            }
            var controller = _controller;
            if (controller == null)
            {
                GUILayout.FlexibleSpace();
                return;
            }
            if (_rowButtonStyle == null)
            {
                var margin = GUI.skin.button.margin;
                _rowButtonStyle = new GUIStyle(GUI.skin.button) { margin = new RectOffset(margin.left, margin.right, 1, 1) };
                _groupStyle = new GUIStyle(_rowButtonStyle) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Clip };
                _slotStyle = new GUIStyle(_rowButtonStyle) { alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
            }

            // Only the rows in view are drawn, like in the maker's menu
            var groups = controller.Groups;
            var type = Event.current.type;
            if (type == EventType.Layout && _builtVersion != AccessoryEvents.Version)
            {
                _builtVersion = AccessoryEvents.Version;
                BuildRows(chaCtrl, groups);
            }

            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            if (type == EventType.Layout)
            {
                _firstRow = Mathf.Clamp((int)(_scroll.y / RowHeight), 0, _rows.Count);
                _endRow = Mathf.Clamp(_firstRow + (int)(_viewHeight / RowHeight) + 2, _firstRow, _rows.Count);
            }
            GUILayout.Space(_firstRow * RowHeight);
            for (var i = _firstRow; i < _endRow; i++)
                DrawRow(chaCtrl, groups, _rows[i], setShown, changed);
            GUILayout.Space((_rows.Count - _endRow) * RowHeight);
            GUILayout.EndScrollView();
            if (type == EventType.Repaint) _viewHeight = GUILayoutUtility.GetLastRect().height;
        }

        // Empty slots are left out
        private void BuildRows(ChaControl chaCtrl, List<SlotGroup> groups)
        {
            _rows.Clear();
            var accessories = AccessoriesApi.GetAccessoryObjects(chaCtrl) ?? new GameObject[0];

            foreach (var group in groups)
            {
                var header = _rows.Count;
                _rows.Add(new Row { Group = group, Slot = -1 });
                bool? groupShown = null;
                for (var slot = group.First; slot <= group.Last && slot < accessories.Length; slot++)
                {
                    if (accessories[slot] == null) continue;
                    var shown = accessories[slot].activeSelf;
                    groupShown = groupShown == true || shown;
                    if (!group.Collapsed) _rows.Add(SlotRow(group, slot, accessories[slot]));
                }
                _rows[header] = new Row { Group = group, Slot = -1, Text = $"{group.Name}  ({group.First + 1}–{group.Last + 1})", Shown = groupShown };
            }

            var ungroupedHeader = _rows.Count;
            bool? ungroupedShown = null;
            var next = 0;
            for (var slot = 0; slot < accessories.Length; slot++)
            {
                while (next < groups.Count && groups[next].Last < slot) next++;
                if (next < groups.Count && groups[next].First <= slot) continue;
                if (accessories[slot] == null) continue;
                ungroupedShown = ungroupedShown == true || accessories[slot].activeSelf;
                if (!_ungroupedCollapsed) _rows.Add(SlotRow(null, slot, accessories[slot]));
            }
            if (ungroupedShown != null) _rows.Insert(ungroupedHeader, new Row { Slot = -1, Text = "Ungrouped", Shown = ungroupedShown });
        }

        private static Row SlotRow(SlotGroup group, int slot, GameObject accessory)
        {
            var info = accessory.GetComponent<ListInfoComponent>();
            var name = info == null || info.data == null ? "" : info.data.Name;
            return new Row { Group = group, Slot = slot, Text = $"{slot + 1}.  {name}", Shown = accessory.activeSelf };
        }

        private void DrawRow(ChaControl chaCtrl, List<SlotGroup> groups, Row row, Action<int, bool> setShown, Action changed)
        {
            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.Height(RowHeight));
            if (row.Slot < 0)
            {
                var collapsed = row.Group == null ? _ungroupedCollapsed : row.Group.Collapsed;
                if (GUILayout.Button((collapsed ? "+  " : "-  ") + row.Text, _groupStyle, GUILayout.MinWidth(0), GUILayout.Height(ButtonHeight)))
                {
                    if (row.Group == null) _ungroupedCollapsed = !_ungroupedCollapsed;
                    else row.Group.Collapsed = !row.Group.Collapsed;
                    _builtVersion = -1;
                }
            }
            else
            {
                GUILayout.Space(24);
                GUILayout.Label(row.Text, _slotStyle, GUILayout.MinWidth(0), GUILayout.Height(ButtonHeight));
            }
            if (row.Shown != null && GUILayout.Button(row.Shown.Value ? "Hide" : "Show", _rowButtonStyle, GUILayout.Width(44), GUILayout.Height(ButtonHeight)))
            {
                if (row.Slot >= 0) setShown(row.Slot, !row.Shown.Value);
                else ShowSlots(chaCtrl, groups, row.Group, !row.Shown.Value, setShown);
                changed();
            }
            GUILayout.EndHorizontal();
        }

        // All slots of the group, or every slot outside groups when group is null
        private static void ShowSlots(ChaControl chaCtrl, List<SlotGroup> groups, SlotGroup group, bool show, Action<int, bool> setShown)
        {
            var accessories = AccessoriesApi.GetAccessoryObjects(chaCtrl) ?? new GameObject[0];
            for (var slot = 0; slot < accessories.Length; slot++)
            {
                if (accessories[slot] == null) continue;
                var inside = group != null ? group.Contains(slot) : SlotGroups.FindOverlap(groups, slot, slot, null) == null;
                if (inside) setShown(slot, show);
            }
        }
    }
}
