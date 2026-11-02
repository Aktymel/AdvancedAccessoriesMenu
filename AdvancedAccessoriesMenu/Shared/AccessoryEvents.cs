using System;
using System.Linq;
using HarmonyLib;
using KKAPI.Maker;
#if HS2 || AI
using AIChara;
#endif

namespace AdvancedAccessoriesMenu
{
    // Version goes up whenever accessories, their names, their visibility or the groups may have changed,
    // so the lists rebuild their rows only then
    internal static class AccessoryEvents
    {
        private static readonly string[] ChaControlMethods =
        {
            "SetAccessoryState", "SetAccessoryStateAll", "SetAccessoryStateCategory",
            "ChangeAccessory", "ChangeAccessoryNoAsync", "ChangeAccessoryColor",
            "ChangeNowCoordinate", "ChangeCoordinateType",
        };

        public static int Version { get; private set; }

        public static void Changed()
        {
            Version++;
        }

        public static void Init(Harmony harmony)
        {
            var postfix = new HarmonyMethod(typeof(AccessoryEvents), nameof(Postfix));
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(ChaControl)).Where(m => ChaControlMethods.Contains(m.Name) && !m.IsGenericMethod))
                harmony.Patch(method, postfix: postfix);

            AccessoriesApi.SelectedMakerAccSlotChanged += (sender, e) => Changed();
            AccessoriesApi.AccessoryKindChanged += (sender, e) => Changed();
            AccessoriesApi.AccessoryTransferred += (sender, e) => Changed();
#if KKS || KK
            AccessoriesApi.AccessoriesCopied += (sender, e) => Changed();
            AccessoriesApi.MakerAccSlotAdded += (sender, e) => Changed();
#endif
            MakerAPI.ReloadCustomInterface += (sender, e) => Changed();
        }

        private static void Postfix()
        {
            Version++;
        }
    }
}
