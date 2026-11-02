using System.Linq;
using HarmonyLib;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using Studio;
using UniRx;
using UnityEngine;

namespace AdvancedAccessoriesMenu
{
    // Groups of the selected Studio character, for hiding and showing accessories
    internal sealed class StudioWindow
    {
        private const int WindowId = 0x41414D32;

        private readonly GroupsListView _list = new GroupsListView();
        private Rect _rect = new Rect(Screen.width - 420, 120, 320, 560);
        private CurrentStateCategorySwitch _switch;
        private OCIChar _selected;

        public bool Visible;

        public void Register()
        {
            var category = StudioAPI.GetOrCreateCurrentStateCategory(AdvancedAccessoriesMenuPlugin.PluginDisplayName);
            _switch = category.AddControl(new CurrentStateCategorySwitch("Groups", c => Visible));
            _switch.Value.Subscribe(value =>
            {
                Visible = value;
                UpdateSelected();
            });
            StudioSaveLoadApi.ObjectsSelected += (sender, e) => UpdateSelected();
            StudioSaveLoadApi.ObjectDeleted += (sender, e) => UpdateSelected();
            StudioSaveLoadApi.SceneLoad += (sender, e) => UpdateSelected();
        }

        private void UpdateSelected()
        {
            _selected = StudioAPI.StudioLoaded ? StudioAPI.GetSelectedCharacters().FirstOrDefault() : null;
        }

        public void OnGUI()
        {
            if (!Visible) return;

            var skin = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
            _rect = GUILayout.Window(WindowId, _rect, DrawWindow, "Accessories menu");
            IMGUIUtils.EatInputInRect(_rect);
            GUI.skin = skin;
        }

        private void DrawWindow(int id)
        {
            var ociChar = _selected != null && _selected.charInfo != null ? _selected : null;

            GUILayout.BeginHorizontal();
            GUILayout.Label(ociChar == null ? "No character selected." : ociChar.charInfo.fileParam.fullname);
            GUILayout.FlexibleSpace();
            if (ConfigManagerBridge.Available && GUILayout.Button("s", GUILayout.Width(26))) ConfigManagerBridge.Open(AdvancedAccessoriesMenuPlugin.PluginDisplayName);
            if (GUILayout.Button("X", GUILayout.Width(26))) _switch.Value.OnNext(false);
            GUILayout.EndHorizontal();

            if (ociChar != null) _list.Draw(ociChar.charInfo, (slot, show) => SetShown(ociChar, slot, show), () => RefreshStatePanel(ociChar));
            else GUILayout.FlexibleSpace();
            GUI.DragWindow();
        }

        // OCIChar.ShowAccessory only knows the game's own slots; MoreAccessories handles its slots in SetAccessoryState
        private static void SetShown(OCIChar ociChar, int slot, bool show)
        {
            if (slot < ociChar.charInfo.fileStatus.showAccessory.Length) ociChar.ShowAccessory(slot, show);
            else ociChar.charInfo.SetAccessoryState(slot, show);
        }

        // The State panel's accessory buttons show the old state until they are updated
        private static void RefreshStatePanel(OCIChar ociChar)
        {
            var panel = Object.FindObjectOfType<MPCharCtrl>();
            if (panel == null) return;
            var stateInfo = Traverse.Create(panel).Field("stateInfo");
            if (stateInfo.GetValue() != null) stateInfo.Method("UpdateInfo", new object[] { ociChar }).GetValue();
        }
    }
}
