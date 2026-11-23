using System.Collections.Generic;
using KKAPI.Maker;
using UnityEngine;

namespace AdvancedAccessoriesMenu
{
    // Each method returns a short error for the menu, or null when it worked
    internal static class SlotMoves
    {
        public static string MoveGroup(List<SlotGroup> groups, SlotGroup group, int first)
        {
            var offset = first - group.First;
            if (offset == 0) return null;
            var last = group.Last + offset;
            if (first < 0) return "Slots start at 1.";
            if (last >= AccessoriesApi.GetMakerAccessoryCount()) return "Not enough slots.";
            var other = SlotGroups.FindOverlap(groups, first, last, group);
            if (other != null) return $"Overlaps \"{other.Name}\".";
            for (var slot = first; slot <= last; slot++)
                if (!group.Contains(slot) && !MakerSlots.IsEmpty(slot)) return $"Slot {slot + 1} is not empty.";

            // Starting from the end the group moves towards, no slot is overwritten before it has moved
            Diag.LogSlots($"Move group {group.First + 1}-{group.Last + 1} to {first + 1}, before");
            var size = group.Last - group.First;
            for (var i = 0; i <= size; i++)
            {
                var source = offset > 0 ? group.Last - i : group.First + i;
                if (MakerSlots.IsEmpty(source)) continue;
                var moved = SlotMover.Move(source, source + offset);
                Diag.LogSlots($"Moved {source + 1} -> {source + offset + 1}{(moved ? "" : " FAILED")}");
                if (!moved) return "Moving failed.";
            }
            group.First = first;
            group.Last = last;
            SlotGroups.Sort(groups);
            return null;
        }

        // A new group with copies of the accessories, starting at the given slot
        public static string CopyGroup(List<SlotGroup> groups, SlotGroup group, int first)
        {
            var last = first + group.Last - group.First;
            if (first < 0) return "Slots start at 1.";
            if (last >= AccessoriesApi.GetMakerAccessoryCount()) return "Not enough slots.";
            var other = SlotGroups.FindOverlap(groups, first, last, null);
            if (other != null) return $"Overlaps \"{other.Name}\".";
            for (var slot = first; slot <= last; slot++)
                if (!MakerSlots.IsEmpty(slot)) return $"Slot {slot + 1} is not empty.";

            Diag.LogSlots($"Copy group {group.First + 1}-{group.Last + 1} to {first + 1}, before");
            for (var source = group.First; source <= group.Last; source++)
            {
                if (MakerSlots.IsEmpty(source)) continue;
                var copied = SlotMover.Copy(source, first + source - group.First);
                Diag.LogSlots($"Copied {source + 1} -> {first + source - group.First + 1}{(copied ? "" : " FAILED")}");
                if (!copied) return "Copying failed.";
            }
            groups.Add(new SlotGroup { Name = group.Name + " copy", First = first, Last = last });
            SlotGroups.Sort(groups);
            return null;
        }

#if KKS || KK
        // Into the same slots of another outfit, which must be empty and outside its groups
        public static string CopyGroupToOutfit(GroupsController controller, SlotGroup group, int outfit, string outfitName)
        {
            var groups = controller.OutfitGroups(outfit);
            var other = SlotGroups.FindOverlap(groups, group.First, group.Last, null);
            if (other != null) return $"Overlaps \"{other.Name}\" in {outfitName}.";
            var count = AccessoriesApi.GetMakerAccessoryCount();
            var targetCount = MakerSlots.SlotCount(outfit);
            var last = Mathf.Min(group.Last, count - 1);
            for (var slot = group.First; slot <= last; slot++)
            {
                if (slot >= targetCount)
                {
                    if (!MakerSlots.IsEmpty(slot)) return $"Not enough slots in {outfitName}.";
                    continue;
                }
                if (!MakerSlots.IsEmpty(outfit, slot)) return $"Slot {slot + 1} in {outfitName} is not empty.";
            }

            var any = false;
            for (var slot = group.First; slot <= last; slot++)
                if (!MakerSlots.IsEmpty(slot)) any = true;
            if (any)
            {
                var error = SlotMover.CopyToOutfit(group.First, last, outfit);
                if (error != null) return error;
            }
            groups.Add(new SlotGroup { Name = group.Name, First = group.First, Last = group.Last, Collapsed = group.Collapsed });
            SlotGroups.Sort(groups);
            return null;
        }
#endif

        // Empty slots of a group, or of all slots outside groups when group is null
        public static int FreeSlots(List<SlotGroup> groups, SlotGroup group)
        {
            var count = AccessoriesApi.GetMakerAccessoryCount();
            var free = 0;
            for (var slot = 0; slot < count; slot++)
            {
                var inside = group != null ? group.Contains(slot) : SlotGroups.FindOverlap(groups, slot, slot, null) == null;
                if (inside && MakerSlots.IsEmpty(slot)) free++;
            }
            return free;
        }

        // Into the first free slot of the group
        public static string MoveToGroup(int slot, SlotGroup group)
        {
            if (group.Contains(slot)) return null;
            var count = AccessoriesApi.GetMakerAccessoryCount();
            for (var target = group.First; target <= group.Last && target < count; target++)
                if (MakerSlots.IsEmpty(target)) return SlotMover.Move(slot, target) ? null : "Moving failed.";
            return $"No free slot in \"{group.Name}\".";
        }

        // Into the first free slot outside every group
        public static string MoveToUngrouped(List<SlotGroup> groups, int slot)
        {
            var count = AccessoriesApi.GetMakerAccessoryCount();
            for (var target = 0; target < count; target++)
                if (SlotGroups.FindOverlap(groups, target, target, null) == null && MakerSlots.IsEmpty(target))
                    return SlotMover.Move(slot, target) ? null : "Moving failed.";
            return "No free ungrouped slot.";
        }
    }
}
