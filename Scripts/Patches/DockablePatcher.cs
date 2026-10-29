using HarmonyLib;

namespace VehicleFramework.Patches
{
    // BZ compatibility: Dockable owns undocking completion; forward it to the existing framework lifecycle.
    [HarmonyPatch(typeof(Dockable))]
    internal static class DockablePatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Dockable.OnUndockingComplete))]
        private static void OnUndockingCompletePostfix(Dockable __instance)
        {
            ModVehicle vehicle = __instance.vehicle as ModVehicle;
            if (vehicle != null && vehicle.IsDocked)
            {
                vehicle.OnVehicleUndocked();
            }
        }
    }
}
