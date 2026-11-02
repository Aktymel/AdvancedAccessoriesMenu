using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;

namespace AdvancedAccessoriesMenu
{
    [BepInPlugin(GUID, PluginDisplayName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Accessory_Shortcuts", BepInDependency.DependencyFlags.SoftDependency)]
#if HS2
    [BepInProcess("HoneySelect2")]
    [BepInProcess("StudioNEOV2")]
#elif AI
    [BepInProcess("AI-Syoujyo")]
    [BepInProcess("AI-Shoujo")]
    [BepInProcess("StudioNEOV2")]
#elif KKS
    [BepInProcess("KoikatsuSunshine")]
    [BepInProcess("CharaStudio")]
#elif KK
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("CharaStudio")]
#endif
    public class AdvancedAccessoriesMenuPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.aktymel.advancedaccessoriesmenu";
        public const string PluginDisplayName = "Advanced Accessories Menu";
        public const string Version = "0.2.0";

        internal static new ManualLogSource Logger;

        internal ConfigEntry<bool> HideEmptySlots;
        private ConfigEntry<bool> _openAutomatically;
        private AccessoriesMenuWindow _window;
        private readonly StudioWindow _studioWindow = new StudioWindow();
#if HS2 || KKS || KK
        private readonly HSceneWindow _hSceneWindow = new HSceneWindow();
#endif

        private void Awake()
        {
            Logger = base.Logger;
            _openAutomatically = Config.Bind("General", "Open automatically", false, "Show the Accessories menu instead of the game's slot list when the maker opens.");
            HideEmptySlots = Config.Bind("General", "Hide empty slots", false, "Leave empty accessory slots out of the Accessories menu.");
            _window = new AccessoriesMenuWindow(this);

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            AccessoryEvents.Init(harmony);
            HideEmptySlots.SettingChanged += (sender, e) => AccessoryEvents.Changed();
            NativeMenu.Changed += ApplyMakerState;
            try
            {
                Hooks.PatchMoreAccessories(harmony);
            }
            catch (Exception e)
            {
                // A MoreAccessories version this wasn't made for only loses the slot name fix
                Logger.LogWarning("Could not patch MoreAccessories: " + e);
            }
            try
            {
                AccessoryShortcutsBlock.Patch(harmony);
            }
            catch (Exception e)
            {
                Logger.LogWarning("Could not patch Accessory Shortcuts: " + e);
            }
            CharacterApi.RegisterExtraBehaviour<GroupsController>(GUID);
            if (StudioAPI.InsideStudio) _studioWindow.Register();
            MakerAPI.MakerFinishedLoading += (sender, e) =>
            {
                MakerSlots.Reset();
                NativeMenu.Reset();
                _window.Visible = _openAutomatically.Value;
                NativeMenu.Attach();
            };
            MakerAPI.MakerExiting += (sender, e) =>
            {
                _window.Reset();
                MakerSlots.Reset();
                NativeMenu.Reset();
                AccessoryHighlight.Reset();
            };
        }

        private bool WindowShown => _window.Visible && NativeMenu.Shown;

        private void ApplyMakerState()
        {
            NativeMenu.SetListHidden(WindowShown);
            if (!WindowShown) AccessoryHighlight.Show(-1);
        }

        private void OnGUI()
        {
            if (StudioAPI.StudioLoaded) _studioWindow.OnGUI();
#if HS2 || KKS || KK
            _hSceneWindow.OnGUI();
#endif
            if (!MakerAPI.InsideAndLoaded || !NativeMenu.Shown) return;
            if (NativeMenu.DrawToggleButton(_window.Visible))
            {
                _window.Visible = !_window.Visible;
                ApplyMakerState();
            }
            if (_window.Visible) _window.OnGUI(NativeMenu.ListRect);
        }
    }
}
