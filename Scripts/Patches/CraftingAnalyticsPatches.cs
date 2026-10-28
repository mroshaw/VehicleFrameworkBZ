using HarmonyLib;
using VehicleFramework.Admin;
using VehicleFramework.VehicleBuilding;

namespace VehicleFramework.Patches
{
    [HarmonyPatch(typeof(CraftingAnalytics))]
    internal static class CraftingAnalyticsPatches
    {
        [HarmonyPatch(nameof(CraftingAnalytics.OnLockedChange))]
        [HarmonyPrefix]
        private static bool OnLockedChange_Prefix(CraftingAnalytics __instance, PDAScanner.Entry entry)
        {
            if (entry is null || PDAScanner.GetEntryData(entry.techType) is not null)
            {
                return true;
            }

            foreach (VehicleEntry vehicle in VehicleManager.vehicleTypes)
            {
                if (vehicle.techType != entry.techType)
                {
                    continue;
                }

                // SN falls back to the blueprint's own TechType when scanner metadata
                // is absent. BZ dereferences that metadata, crashing on our partial entries.
                if (__instance._active && entry.techType != TechType.None && !__instance.toUpdate.Contains(entry.techType))
                {
                    __instance.toUpdate.Add(entry.techType);
                }
                return false;
            }
            return true;
        }
    }
}
