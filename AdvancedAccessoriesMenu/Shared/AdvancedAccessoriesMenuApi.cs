#if HS2 || AI
using AIChara;
#endif

namespace AdvancedAccessoriesMenu
{
    // For other plugins, called by reflection, so the names must stay. Groups are text in the SlotGroups.Encode format,
    // outfit is the coordinateType (always 0 in HS2/AI).
    public static class AdvancedAccessoriesMenuApi
    {
        // Null when the outfit has no groups
        public static string GetGroups(ChaControl chaCtrl, int outfit)
        {
            var controller = GroupsController.Get(chaCtrl);
            return controller == null ? null : controller.GetGroups(outfit);
        }

        // Null or empty removes the outfit's groups, overlapping groups are dropped
        public static void SetGroups(ChaControl chaCtrl, int outfit, string groups)
        {
            var controller = GroupsController.Get(chaCtrl);
            if (controller != null) controller.SetGroups(outfit, groups);
        }
    }
}
