using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
#if HS2 || AI
using AIChara;
#endif

namespace AdvancedAccessoriesMenu
{
    internal sealed class GroupsController : CharaCustomFunctionController
    {
        private const int DataVersion = 1;
        private const string OutfitKeyPrefix = "outfit";
        private const string CoordinateKey = "groups";

        private readonly Dictionary<int, List<SlotGroup>> _groups = new Dictionary<int, List<SlotGroup>>();

        public static GroupsController Get(ChaControl chaCtrl)
        {
            return chaCtrl == null ? null : chaCtrl.GetComponent<GroupsController>();
        }

        public List<SlotGroup> Groups
        {
            get
            {
                List<SlotGroup> groups;
                if (!_groups.TryGetValue(CurrentOutfit, out groups))
                {
                    groups = new List<SlotGroup>();
                    _groups[CurrentOutfit] = groups;
                }
                return groups;
            }
        }

        public string GetGroups(int outfit)
        {
            List<SlotGroup> groups;
            return _groups.TryGetValue(outfit, out groups) && groups.Count > 0 ? SlotGroups.Encode(groups) : null;
        }

        // A new list, so the menu sees another list and drops what its editor had open
        public void SetGroups(int outfit, string groups)
        {
            _groups[outfit] = SlotGroups.Decode(groups);
            AccessoryEvents.Changed();
        }

        private int CurrentOutfit
        {
            get
            {
#if KKS || KK
                return ChaControl.fileStatus.coordinateType;
#else
                return 0;
#endif
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            var data = new PluginData { version = DataVersion };
            foreach (var pair in _groups.Where(p => p.Value.Count > 0))
                data.data[OutfitKeyPrefix + pair.Key] = SlotGroups.Encode(pair.Value);
            SetExtendedData(data.data.Count > 0 ? data : null);
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (maintainState) return;
            // Loading a character without its clothes keeps the current accessories, so their groups stay too
            if (MakerAPI.InsideAndLoaded)
            {
                var flags = MakerAPI.GetCharacterLoadFlags();
                if (flags != null && !flags.Clothes) return;
            }

            _groups.Clear();
            AccessoryEvents.Changed();
            var data = GetExtendedData();
            if (data == null) return;
            foreach (var pair in data.data)
            {
                int outfit;
                if (!pair.Key.StartsWith(OutfitKeyPrefix) || !int.TryParse(pair.Key.Substring(OutfitKeyPrefix.Length), out outfit)) continue;
                _groups[outfit] = SlotGroups.Decode(pair.Value as string);
            }
        }

        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {
            var groups = Groups;
            if (groups.Count == 0)
            {
                SetCoordinateExtendedData(coordinate, null);
                return;
            }
            var data = new PluginData { version = DataVersion };
            data.data[CoordinateKey] = SlotGroups.Encode(groups);
            SetCoordinateExtendedData(coordinate, data);
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            if (maintainState) return;
            if (MakerAPI.InsideAndLoaded)
            {
                var flags = MakerAPI.GetCoordinateLoadFlags();
                if (flags != null && !flags.Accessories) return;
            }

            var data = GetCoordinateExtendedData(coordinate);
            object value = null;
            if (data != null) data.data.TryGetValue(CoordinateKey, out value);
            _groups[CurrentOutfit] = SlotGroups.Decode(value as string);
            AccessoryEvents.Changed();
        }
    }
}
