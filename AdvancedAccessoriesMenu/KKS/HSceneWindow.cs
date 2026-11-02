#if KKS || KK
using System.Collections.Generic;
using System.Text;
using KKAPI.Utilities;
using UnityEngine;

namespace AdvancedAccessoriesMenu
{
    // H scene: "Advanced" next to a girl's accessory buttons swaps them for her groups
    internal sealed class HSceneWindow
    {
        private const int WindowId = 0x41414D33;
        private const float ButtonWidth = 110;
        private const float ButtonHeight = 32;
        private const float ButtonGap = 8;
        private const float WindowWidth = 320;
        private const float WindowHeight = 480;

        internal static HSceneWindow Instance;

        private readonly GroupsListView _list = new GroupsListView();
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private readonly List<CanvasGroup> _hiddenGroups = new List<CanvasGroup>();
        private List<ChaControl> _females;
        private Candidate _panel;
        private Transform _hiddenTarget;
        private bool _advanced;

        public HSceneWindow()
        {
            Instance = this;
        }

        // One girl's accessory buttons; MoreAccessories later moves them into a scroll view.
        // All is the column of Show All / Remove All… next to them.
        private sealed class Candidate
        {
            public HSceneSpriteCategory Category;
            public Transform All;
            public int Female;
            public Transform Target;
            public Camera Camera;
        }

        // From the HSprite.Init postfix
        public void SetSprite(HSprite sprite, List<ChaControl> females)
        {
            _candidates.Clear();
            _females = females;
            if (sprite != null && females != null)
            {
                Add(sprite.categoryAccessory, sprite.categoryAccessoryAll, 0);
                if (sprite.lstMultipleFemaleDressButton != null)
                    for (var i = 0; i < sprite.lstMultipleFemaleDressButton.Count; i++)
                        Add(sprite.lstMultipleFemaleDressButton[i].accessory, sprite.lstMultipleFemaleDressButton[i].accessoryAll, i);
            }
            foreach (var candidate in _candidates)
                UiWatcher.Watch(candidate.Category.gameObject, Refresh);
            Refresh();
        }

        public void OnGUI()
        {
            var panel = _panel;
            if (panel == null || panel.Target == null) return;
            var chaCtrl = _females[panel.Female];
            if (chaCtrl == null) return;

            // Above the accessory buttons, next to the Clothes button
            var rect = ScreenUi.ScreenRect((RectTransform)panel.Target, panel.Camera);
            var scale = ScreenUi.Scale;
            var size = new Vector2(ButtonWidth * scale, ButtonHeight * scale);
            var button = new Rect(rect.x, rect.y - size.y - ButtonGap * scale, size.x, size.y);
            if (ScreenUi.DrawToggleButton(button, _advanced))
            {
                _advanced = !_advanced;
                ApplyHidden();
            }
            if (!_advanced) return;

            var skin = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
            // Over the accessory buttons and the Show All column
            var right = rect.xMax;
            var bottom = rect.yMax;
            if (panel.All != null)
            {
                var all = ScreenUi.ScreenRect((RectTransform)panel.All, panel.Camera);
                right = Mathf.Max(right, all.xMax);
                bottom = Mathf.Max(bottom, all.yMax);
            }
            var window = new Rect(rect.x, rect.y, Mathf.Max(right - rect.x, WindowWidth), Mathf.Max(bottom - rect.y, WindowHeight));
            GUILayout.Window(WindowId, window, id => DrawWindow(chaCtrl), "Accessories menu");
            IMGUIUtils.EatInputInRect(window);
            GUI.skin = skin;
        }

        private void DrawWindow(ChaControl chaCtrl)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(chaCtrl.fileParam.fullname);
            GUILayout.FlexibleSpace();
            if (ConfigManagerBridge.Available && GUILayout.Button("s", GUILayout.Width(26))) ConfigManagerBridge.Open(AdvancedAccessoriesMenuPlugin.PluginDisplayName);
            GUILayout.EndHorizontal();
            // The game shows accessories by fileStatus.showAccessory, which SetAccessoryState sets; its buttons have no on/off look
            _list.Draw(chaCtrl, (slot, show) => chaCtrl.SetAccessoryState(slot, show), () => { });
        }

        private void Add(HSceneSpriteCategory category, HSceneSpriteCategory all, int female)
        {
            if (category != null && female < _females.Count)
                _candidates.Add(new Candidate { Category = category, All = all == null ? null : all.transform, Female = female });
        }

        private void Refresh()
        {
            var old = _panel;
            _panel = null;
            foreach (var candidate in _candidates)
            {
                if (candidate.Category == null || !candidate.Category.gameObject.activeInHierarchy || _females[candidate.Female] == null) continue;
                var target = candidate.Category.transform;
                var scroll = target.parent == null ? null : target.parent.parent;
                if (scroll != null && scroll.name == "AccessoryScroll") target = scroll;
                // Our own CanvasGroup on the target doesn't count
                if (!ScreenUi.IsShown(target.parent)) continue;
                candidate.Target = target;
                candidate.Camera = ScreenUi.CanvasCamera(target);
                _panel = candidate;
                break;
            }
            ApplyHidden();
            if (_panel != old) LogPanel();
        }

        // The window lies over both columns, so neither may take its clicks.
        // Hiding raises OnCanvasGroupChanged and so another refresh, which then has nothing to do.
        private void ApplyHidden()
        {
            var hide = _advanced && _panel != null ? _panel.Target : null;
            if (hide == _hiddenTarget) return;
            _hiddenTarget = hide;
            foreach (var group in _hiddenGroups)
                if (group != null) ScreenUi.Hide(group, false);
            _hiddenGroups.Clear();
            if (hide == null) return;
            foreach (var t in new[] { _panel.Target, _panel.All })
            {
                if (t == null) continue;
                var group = ScreenUi.GetOrAddCanvasGroup(t.gameObject);
                ScreenUi.Hide(group, true);
                _hiddenGroups.Add(group);
            }
        }

        // Where the panel is and what's around it, to place the window
        [System.Diagnostics.Conditional("DEBUG")]
        private void LogPanel()
        {
            if (_panel == null)
            {
                Diag.Log("H accessory panel closed");
                return;
            }
            var target = _panel.Target;
            var sb = new StringBuilder("H accessory panel ").Append(target.name).Append(' ').Append(ScreenUi.ScreenRect((RectTransform)target, _panel.Camera));
            if (target.parent != null)
                foreach (Transform sibling in target.parent)
                {
                    sb.Append("\n  ").Append(sibling.name).Append(sibling.gameObject.activeSelf ? "" : " (inactive)");
                    if (sibling is RectTransform rt) sb.Append(' ').Append(ScreenUi.ScreenRect(rt, _panel.Camera));
                }
            Diag.Log(sb.ToString());
        }
    }
}
#endif
