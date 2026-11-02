using System.Collections.Generic;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;
#if HS2 || AI
using AIChara;
#endif

namespace AdvancedAccessoriesMenu
{
    internal sealed class AccessoriesMenuWindow
    {
        private const int WindowId = 0x41414D31;
        private const string TextFieldName = "AdvancedAccessoriesMenuText";
        // Slots the game has without MoreAccessories
        private const int VanillaSlotCount = 20;
        private static readonly Color SelectedColor = new Color(0.4f, 0.8f, 1f);

        private const float RowHeight = 24;
        private const float ButtonHeight = 22;

        // A group header (Slot is -1) or a slot; Group is null for "Ungrouped". Shown is null when a group has no accessory.
        private struct Row
        {
            public SlotGroup Group;
            public int Slot;
            public string Text;
            public bool? Shown;
            public bool Selected;
            public bool Empty;
        }

        private readonly AdvancedAccessoriesMenuPlugin _plugin;
        private readonly List<Row> _rows = new List<Row>();
        // Free slots of each group and, last, of Ungrouped, for moving a slot
        private readonly List<int> _freeSlots = new List<int>();
        private int _builtVersion = -1;
        private ChaControl _chaCtrl;
        private GroupsController _controller;
        private int _firstRow;
        private int _endRow;
        private float _viewHeight = 600;
        private int _hoveredSlot = -1;
        // A text field of the menu has the keyboard
        private bool _typing;
        private Vector2 _scroll;
        private bool _ungroupedCollapsed;

        // Group editor; _editing is null while adding a new group. _editorGroups is the list it works on, and
        // _suggestFirst asks for the first free slot to be filled in for the next new group.
        private List<SlotGroup> _editorGroups;
        private bool _suggestFirst = true;
        private SlotGroup _editing;
        private bool _confirmDelete;
        private string _moveTo = "";
        // The slot whose "→" was pressed, -1 when no slot is being moved
        private int _movingSlot = -1;
        private string _name = "";
        private string _first = "";
        private string _last = "";
        private string _error;

        private GUIStyle _headerStyle;
        private GUIStyle _rowButtonStyle;
        private GUIStyle _groupStyle;
        private GUIStyle _slotStyle;
        private GUIStyle _warningStyle;

        public bool Visible;

        public AccessoriesMenuWindow(AdvancedAccessoriesMenuPlugin plugin)
        {
            _plugin = plugin;
        }

        public void Reset()
        {
            Visible = false;
            ClearEditor();
            _chaCtrl = null;
            _controller = null;
            _builtVersion = -1;
        }

        // Drawn over the game's slot list, so it can't be dragged or resized
        public void OnGUI(Rect rect)
        {
            if (!Visible) return;

            var skin = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
            if (_headerStyle == null) _headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            if (_rowButtonStyle == null)
            {
                // Rows have a fixed height, so buttons in them keep only a thin vertical margin
                var margin = GUI.skin.button.margin;
                _rowButtonStyle = new GUIStyle(GUI.skin.button) { margin = new RectOffset(margin.left, margin.right, 1, 1) };
                _groupStyle = new GUIStyle(_rowButtonStyle) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Clip };
                _slotStyle = new GUIStyle(_rowButtonStyle) { alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
            }
            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
                _warningStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            }
            // A click outside the menu takes the keyboard away from its text fields, so other plugins' hotkeys work again
            if (_typing && Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = 0;
                _typing = false;
            }
            GUILayout.Window(WindowId, rect, DrawWindow, "Accessories menu", GUILayout.Width(rect.width), GUILayout.Height(rect.height));
            IMGUIUtils.EatInputInRect(rect);
            GUI.skin = skin;
        }

        private static string TextField(string text, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(TextFieldName);
            return GUILayout.TextField(text, options);
        }

        private void DrawWindow(int id)
        {
            _typing = GUI.GetNameOfFocusedControl() == TextFieldName;
            GUILayout.BeginHorizontal();
            var hideEmpty = GUILayout.Toggle(_plugin.HideEmptySlots.Value, " Hide empty slots");
            if (hideEmpty != _plugin.HideEmptySlots.Value)
            {
                _plugin.HideEmptySlots.Value = hideEmpty;
                _builtVersion = -1;
            }
            GUILayout.FlexibleSpace();
            if (ConfigManagerBridge.Available && GUILayout.Button("s", GUILayout.Width(26))) ConfigManagerBridge.Open(AdvancedAccessoriesMenuPlugin.PluginDisplayName);
            GUILayout.EndHorizontal();
            var warning = SlotMover.Warning;
            if (warning != null) GUILayout.Label(warning, _warningStyle);

            var chaCtrl = MakerAPI.GetCharacterControl();
            if (chaCtrl != _chaCtrl)
            {
                _chaCtrl = chaCtrl;
                _controller = GroupsController.Get(chaCtrl);
                _builtVersion = -1;
            }
            var controller = _controller;
            if (controller == null)
            {
                GUILayout.Label("No character.");
                return;
            }

            var groups = controller.Groups;
            // Another character or outfit
            if (groups != _editorGroups)
            {
                ClearEditor();
                _editorGroups = groups;
                _builtVersion = -1;
            }

            DrawList(groups);
            DrawNativeButtons();
            if (_movingSlot >= 0) DrawSlotMove(groups);
            else DrawEditor(groups);
        }

        // With hundreds of slots only the rows in view are drawn; the rest is empty space of the same height.
        // The rows are built in the Layout event and reused by the other events of the frame, so they always match.
        private void DrawList(List<SlotGroup> groups)
        {
            var type = Event.current.type;
            if (type == EventType.Layout && _builtVersion != AccessoryEvents.Version)
            {
                _builtVersion = AccessoryEvents.Version;
                BuildRows(groups);
            }
            if (type == EventType.Repaint) _hoveredSlot = -1;

            // Long names are clipped instead of widening the list, so there's no horizontal scrollbar
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            if (type == EventType.Layout)
            {
                _firstRow = Mathf.Clamp((int)(_scroll.y / RowHeight), 0, _rows.Count);
                _endRow = Mathf.Clamp(_firstRow + (int)(_viewHeight / RowHeight) + 2, _firstRow, _rows.Count);
            }
            GUILayout.Space(_firstRow * RowHeight);
            for (var i = _firstRow; i < _endRow; i++)
                DrawRow(_rows[i]);
            GUILayout.Space((_rows.Count - _endRow) * RowHeight);
            GUILayout.EndScrollView();

            if (type == EventType.Repaint)
            {
                var view = GUILayoutUtility.GetLastRect();
                _viewHeight = view.height;
                // Rows at the edges reach out of the view, so the mouse must also be inside it
                if (!view.Contains(Event.current.mousePosition)) _hoveredSlot = -1;
                AccessoryHighlight.Show(_hoveredSlot);
            }
        }

        private void BuildRows(List<SlotGroup> groups)
        {
            _rows.Clear();
            var count = AccessoriesApi.GetMakerAccessoryCount();
            var hideEmpty = _plugin.HideEmptySlots.Value;
            var selected = AccessoriesApi.SelectedMakerAccSlot;

            foreach (var group in groups)
            {
                _rows.Add(new Row { Group = group, Slot = -1, Text = $"{group.Name}  ({group.First + 1}–{group.Last + 1})", Shown = GroupShown(group) });
                if (group.Collapsed) continue;
                for (var slot = group.First; slot <= group.Last && slot < count; slot++)
                    if (!hideEmpty || slot == selected || !MakerSlots.IsEmpty(slot)) _rows.Add(SlotRow(group, slot, selected));
            }

            // Groups are sorted, so one pass finds the slots between them
            var ungroupedHeader = _rows.Count;
            var anyUngrouped = false;
            var next = 0;
            for (var slot = 0; slot < count; slot++)
            {
                while (next < groups.Count && groups[next].Last < slot) next++;
                if (next < groups.Count && groups[next].First <= slot) continue;
                anyUngrouped = true;
                if (!_ungroupedCollapsed && (!hideEmpty || slot == selected || !MakerSlots.IsEmpty(slot))) _rows.Add(SlotRow(null, slot, selected));
            }
            if (anyUngrouped) _rows.Insert(ungroupedHeader, new Row { Slot = -1, Text = "Ungrouped" });

            _freeSlots.Clear();
            if (_movingSlot < 0) return;
            foreach (var group in groups)
                _freeSlots.Add(SlotMoves.FreeSlots(groups, group));
            _freeSlots.Add(SlotMoves.FreeSlots(groups, null));
        }

        private static Row SlotRow(SlotGroup group, int slot, int selected)
        {
            return new Row { Group = group, Slot = slot, Text = $"{slot + 1}.  {MakerSlots.Name(slot) ?? ""}", Selected = slot == selected, Empty = MakerSlots.IsEmpty(slot) };
        }

        private void DrawNativeButtons()
        {
            GUILayout.BeginHorizontal();
            if (NativeMenu.HidesCopy && GUILayout.Button("Copy")) NativeMenu.OpenCopy();
            if (NativeMenu.CanAddSlots)
            {
                if (GUILayout.Button("+1", GUILayout.Width(50))) NativeMenu.AddSlots(false);
                if (GUILayout.Button("+10", GUILayout.Width(50))) NativeMenu.AddSlots(true);
            }
            else if (!AccessoriesApi.MoreAccessoriesInstalled)
            {
                GUILayout.Label("Missing MoreAccessories plugin", _warningStyle);
            }
            GUILayout.EndHorizontal();
        }

        // Every row has the same height, so the rows in view can be found from the scroll position
        private void DrawRow(Row row)
        {
            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.Height(RowHeight));
            if (row.Slot < 0) DrawGroupHeader(row);
            else DrawSlot(row);
            GUILayout.EndHorizontal();
        }

        // Clicking the header collapses or expands the group; Group is null for "Ungrouped"
        private void DrawGroupHeader(Row row)
        {
            var group = row.Group;
            var collapsed = group == null ? _ungroupedCollapsed : group.Collapsed;
            if (GUILayout.Button((collapsed ? "+  " : "-  ") + row.Text, _groupStyle, GUILayout.MinWidth(0), GUILayout.Height(ButtonHeight)))
            {
                if (group == null) _ungroupedCollapsed = !_ungroupedCollapsed;
                else group.Collapsed = !group.Collapsed;
                _builtVersion = -1;
            }
            if (group == null) return;
            var shown = row.Shown;
            if (shown != null && GUILayout.Button(shown.Value ? "Hide" : "Show", _rowButtonStyle, GUILayout.Width(44), GUILayout.Height(ButtonHeight))) ShowGroup(group, !shown.Value);
            if (GUILayout.Button("Edit", _rowButtonStyle, GUILayout.Width(40), GUILayout.Height(ButtonHeight))) StartEdit(group);
        }

        // True when any accessory of the group is visible, null when the group has none
        private static bool? GroupShown(SlotGroup group)
        {
            var chaCtrl = MakerAPI.GetCharacterControl();
            var count = AccessoriesApi.GetMakerAccessoryCount();
            bool? shown = null;
            for (var slot = group.First; slot <= group.Last && slot < count; slot++)
            {
                var accessory = AccessoriesApi.GetAccessoryObject(chaCtrl, slot);
                if (accessory == null) continue;
                if (accessory.activeSelf) return true;
                shown = false;
            }
            return shown;
        }

        private static void ShowGroup(SlotGroup group, bool show)
        {
            var chaCtrl = MakerAPI.GetCharacterControl();
            var count = AccessoriesApi.GetMakerAccessoryCount();
            for (var slot = group.First; slot <= group.Last && slot < count; slot++)
                if (!MakerSlots.IsEmpty(slot)) chaCtrl.SetAccessoryState(slot, show);
        }

        private void DrawSlot(Row row)
        {
            var slot = row.Slot;
            GUILayout.Space(24);
            var color = GUI.backgroundColor;
            if (row.Selected) GUI.backgroundColor = SelectedColor;
            if (GUILayout.Button(row.Text, _slotStyle, GUILayout.MinWidth(0), GUILayout.Height(ButtonHeight))) MakerSlots.Select(slot);
            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) _hoveredSlot = slot;
            GUI.backgroundColor = color;
            if (SlotMover.Available && !row.Empty && GUILayout.Button("→", _rowButtonStyle, GUILayout.Width(24), GUILayout.Height(ButtonHeight)))
            {
                ClearEditor();
                _movingSlot = slot;
                _builtVersion = -1;
            }
        }

        private void DrawSlotMove(List<SlotGroup> groups)
        {
            GUILayout.Label($"Move slot {_movingSlot + 1} to", _headerStyle);
            if (groups.Count == 0) GUILayout.Label("No groups yet.");
            // Built with the rows in the Layout event
            if (_freeSlots.Count != groups.Count + 1) return;
            var inGroup = false;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group.Contains(_movingSlot)) inGroup = true;
                if (GUILayout.Button($"{group.Name}  ({group.First + 1}–{group.Last + 1})  {_freeSlots[i]} free", _slotStyle, GUILayout.MinWidth(0)))
                    FinishSlotMove(SlotMoves.MoveToGroup(_movingSlot, group));
            }
            if (inGroup && GUILayout.Button($"Ungrouped  {_freeSlots[groups.Count]} free", _slotStyle, GUILayout.MinWidth(0)))
                FinishSlotMove(SlotMoves.MoveToUngrouped(groups, _movingSlot));
            if (_error != null) GUILayout.Label(_error, _warningStyle);
            if (GUILayout.Button("Cancel", GUILayout.Height(26))) ClearEditor();
        }

        private void FinishSlotMove(string error)
        {
            AccessoryEvents.Changed();
            _error = error;
            if (_error == null) _movingSlot = -1;
            // The buttons are gone after the move, so this event must not draw the rest of them
            GUIUtility.ExitGUI();
        }

        private void DrawEditor(List<SlotGroup> groups)
        {
            if (_suggestFirst && _editing == null)
            {
                _first = (SlotGroups.FirstFreeSlot(groups) + 1).ToString();
                _suggestFirst = false;
            }

            GUILayout.Label(_editing == null ? "New group" : "Edit group", _headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(44));
            _name = TextField(_name);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Slots", GUILayout.Width(44));
            _first = TextField(_first, GUILayout.Width(50));
            GUILayout.Label("–", GUILayout.Width(14));
            _last = TextField(_last, GUILayout.Width(50));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Moves or copies the group with its accessories so that it starts at the given slot
            if (_editing != null && SlotMover.Available)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("To slot", GUILayout.Width(44));
                _moveTo = TextField(_moveTo, GUILayout.Width(50));
                if (GUILayout.Button("Move", GUILayout.MinWidth(0))) MoveEdited(groups, false);
                if (GUILayout.Button("Copy", GUILayout.MinWidth(0))) MoveEdited(groups, true);
                GUILayout.EndHorizontal();
            }

            if (_error != null) GUILayout.Label(_error, _warningStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_editing == null ? "Add group" : "Save", GUILayout.Height(26), GUILayout.MinWidth(0))) Apply(groups);
            if (_editing != null)
            {
                if (GUILayout.Button(_confirmDelete ? "Sure?" : "Delete", GUILayout.Height(26), GUILayout.Width(56)))
                {
                    if (_confirmDelete) DeleteEdited(groups);
                    else _confirmDelete = true;
                }
                if (GUILayout.Button("Cancel", GUILayout.Height(26), GUILayout.Width(56))) ClearEditor();
            }
            GUILayout.EndHorizontal();
        }

        private void DeleteEdited(List<SlotGroup> groups)
        {
            groups.Remove(_editing);
            ClearEditor();
            AccessoryEvents.Changed();
        }

        private void MoveEdited(List<SlotGroup> groups, bool copy)
        {
            int first;
            if (!int.TryParse(_moveTo.Trim(), out first))
            {
                _error = "Enter a slot number.";
                return;
            }
            var error = copy ? SlotMoves.CopyGroup(groups, _editing, first - 1) : SlotMoves.MoveGroup(groups, _editing, first - 1);
            StartEdit(_editing);
            _error = error;
            AccessoryEvents.Changed();
        }

        private void StartEdit(SlotGroup group)
        {
            ClearEditor();
            _editing = group;
            _name = group.Name;
            _first = (group.First + 1).ToString();
            _last = (group.Last + 1).ToString();
            _moveTo = _first;
        }

        private void ClearEditor()
        {
            _movingSlot = -1;
            _moveTo = "";
            _editing = null;
            _confirmDelete = false;
            _name = "";
            _first = "";
            _last = "";
            _error = null;
            _suggestFirst = true;
        }

        private void Apply(List<SlotGroup> groups)
        {
            int first, last;
            if (!int.TryParse(_first.Trim(), out first) || !int.TryParse(_last.Trim(), out last))
            {
                _error = "Enter the first and the last slot number.";
                return;
            }
            if (first < 1 || last < first)
            {
                _error = "The first slot must be 1 or more and not after the last slot.";
                return;
            }
            if (!AccessoriesApi.MoreAccessoriesInstalled && last > VanillaSlotCount)
            {
                _error = $"Missing MoreAccessories, max slots {VanillaSlotCount}.";
                return;
            }
            first--;
            last--;

            var other = SlotGroups.FindOverlap(groups, first, last, _editing);
            if (other != null)
            {
                _error = $"Slots {first + 1}–{last + 1} overlap the group \"{other.Name}\" ({other.First + 1}–{other.Last + 1}).";
                return;
            }

            var name = SlotGroups.CleanName(_name);
            if (name.Length == 0) name = "Group " + (groups.Count + (_editing == null ? 1 : 0));

            if (_editing == null)
            {
                groups.Add(new SlotGroup { Name = name, First = first, Last = last });
            }
            else
            {
                _editing.Name = name;
                _editing.First = first;
                _editing.Last = last;
            }
            SlotGroups.Sort(groups);
            ClearEditor();
            AccessoryEvents.Changed();
        }
    }
}
