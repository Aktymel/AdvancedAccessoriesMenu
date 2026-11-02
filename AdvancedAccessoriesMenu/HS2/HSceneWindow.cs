#if HS2
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AIChara;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using UnityEngine;

namespace AdvancedAccessoriesMenu
{
    // H scene: "Advanced" above the game's accessory panel swaps it for the groups of the character the panel shows
    internal sealed class HSceneWindow
    {
        private const int WindowId = 0x41414D33;
        private const float ButtonWidth = 110;
        private const float ButtonHeight = 32;
        private const float ButtonGap = 8;
        private const float WindowWidth = 320;
        private const float WindowHeight = 560;

        // Private in the game, public only in the reference assemblies
        private static readonly FieldInfo DropdownField = AccessTools.Field(typeof(HSceneSpriteChaChoice), "dropdown");

        internal static HSceneWindow Instance;

        private readonly GroupsListView _list = new GroupsListView();
        private readonly List<CanvasGroup> _hiddenGroups = new List<CanvasGroup>();
        private HSceneSpriteAccessoryCondition _panel;
        private ChaControl _character;
        private bool _shown;
        private Rect _panelRect;
        private Rect _buttonRect;
        private bool _advanced;

        public HSceneWindow()
        {
            Instance = this;
        }

        // From the postfix on the panel's Init, when an H scene starts
        public void SetPanel(HSceneSpriteAccessoryCondition panel)
        {
            _panel = panel;
            _hiddenGroups.Clear();
            UiWatcher.Watch(panel.gameObject, Refresh);
            CharacterChanged();
        }

        // From the postfix on SetAccessoryCharacter, which the game calls when another character is picked
        public void CharacterChanged()
        {
            _character = CurrentCharacter;
            Refresh();
        }

        public void OnGUI()
        {
            if (_panel == null || !_shown) return;

            if (ScreenUi.DrawToggleButton(_buttonRect, _advanced))
            {
                _advanced = !_advanced;
                ApplyHidden();
            }
            if (!_advanced) return;

            var skin = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
            var rect = new Rect(_panelRect.x, _panelRect.y, Mathf.Max(_panelRect.width, WindowWidth), WindowHeight);
            GUILayout.Window(WindowId, rect, DrawWindow, "Accessories menu");
            IMGUIUtils.EatInputInRect(rect);
            GUI.skin = skin;
        }

        private void DrawWindow(int id)
        {
            var chaCtrl = _character;
            GUILayout.BeginHorizontal();
            GUILayout.Label(chaCtrl == null ? "" : chaCtrl.fileParam.fullname);
            GUILayout.FlexibleSpace();
            if (ConfigManagerBridge.Available && GUILayout.Button("s", GUILayout.Width(26))) ConfigManagerBridge.Open(AdvancedAccessoriesMenuPlugin.PluginDisplayName);
            GUILayout.EndHorizontal();

            if (chaCtrl != null) _list.Draw(chaCtrl, (slot, show) => chaCtrl.SetAccessoryState(slot, show), () => _panel.SetAccessoryCharacter());
            else GUILayout.FlexibleSpace();
        }

        // The character picked in the panel's dropdown: females first, then males
        private static ChaControl CurrentCharacter
        {
            get
            {
                var manager = Singleton<HSceneManager>.Instance;
                if (manager == null || manager.Hscene == null) return null;
                var index = manager.numFemaleClothCustom;
                var characters = index < 2 ? manager.Hscene.GetFemales() : manager.Hscene.GetMales();
                if (index >= 2) index -= 2;
                return characters != null && index < characters.Length ? characters[index] : null;
            }
        }

        private void Refresh()
        {
            var wasShown = _shown;
            _shown = ScreenUi.IsShown(_panel);
            if (_shown)
            {
                _panelRect = ScreenUi.ScreenRect((RectTransform)_panel.transform);
                var scale = ScreenUi.Scale;
                var size = new Vector2(ButtonWidth * scale, ButtonHeight * scale);
                _buttonRect = new Rect(_panelRect.x, _panelRect.y - size.y - ButtonGap * scale, size.x, size.y);
                // Right of the character dropdown, which sits above the list
                var choice = _panel.hSceneSpriteChaChoice;
                var dropdown = choice == null ? null : DropdownField.GetValue(choice) as Component;
                if (dropdown != null)
                {
                    var rect = ScreenUi.ScreenRect((RectTransform)dropdown.transform);
                    _buttonRect = new Rect(rect.xMax + ButtonGap * scale, rect.center.y - size.y / 2, size.x, size.y);
                }
                if (!wasShown) LogPanel();
            }
            ApplyHidden();
        }

        // The panel's parts are hidden, not the panel, so the game still sees it open. The character dropdown stays.
        private void ApplyHidden()
        {
            var hide = _advanced && _shown && _panel != null;
            if (hide == (_hiddenGroups.Count > 0)) return;
            foreach (var group in _hiddenGroups)
                if (group != null) ScreenUi.Hide(group, false);
            _hiddenGroups.Clear();
            if (!hide) return;
            var choice = _panel.hSceneSpriteChaChoice;
            foreach (Transform child in _panel.transform)
            {
                if (choice != null && choice.transform.IsChildOf(child)) continue;
                var group = ScreenUi.GetOrAddCanvasGroup(child.gameObject);
                ScreenUi.Hide(group, true);
                _hiddenGroups.Add(group);
            }
        }

        // The panel's children and where they are, to place the window
        [System.Diagnostics.Conditional("DEBUG")]
        private void LogPanel()
        {
            var sb = new StringBuilder("H accessory panel ").Append(_panelRect);
            foreach (Transform child in _panel.transform)
            {
                sb.Append("\n  ").Append(child.name).Append(child.gameObject.activeSelf ? "" : " (inactive)");
                if (child is RectTransform rt) sb.Append(' ').Append(ScreenUi.ScreenRect(rt));
            }
            Diag.Log(sb.ToString());
        }
    }
}
#endif
