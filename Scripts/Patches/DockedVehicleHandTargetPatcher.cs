using HarmonyLib;
using VehicleFramework.Extensions;
using UnityEngine;
using VehicleFramework.VehicleTypes;

namespace VehicleFramework.Patches
{
    [HarmonyPatch(typeof(DockedVehicleHandTarget))]
    public static class DockedVehicleHandTargetPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandHover))]
        public static void OnHandHoverPostfix(DockedVehicleHandTarget __instance)
        {
            Dockable dockable = __instance.dockingBay.GetDockedObject();
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle == null)
            {
                return;
            }
            float energy = dockable.GetEnergyScalar();
            string status = energy < 1f
                ? Language.main.GetFormat("VehicleStatusFormat", vehicle.liveMixin.GetHealthFraction(), energy)
                : Language.main.GetFormat("VehicleStatusChargedFormat", vehicle.liveMixin.GetHealthFraction());
            CrushDamage crushDamage = vehicle.GetComponent<CrushDamage>();
            if (crushDamage != null && Ocean.GetDepthOf(Player.main.gameObject) > crushDamage.crushDepth)
            {
                status = Language.main.Get("DockedVehicleDepthWarning");
            }
            bool isDrone = vehicle is Drone;
            HandReticle.main.SetText(HandReticle.TextType.Hand, vehicle.HullName, false,
                isDrone ? GameInput.Button.None : GameInput.Button.LeftHand);
            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, status, false, GameInput.Button.None);
            HandReticle.main.SetIcon(isDrone ? HandReticle.IconType.Default : HandReticle.IconType.Hand);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandClick))]
        public static bool OnHandClickPrefix(DockedVehicleHandTarget __instance)
        {
            Dockable dockable = __instance.dockingBay.GetDockedObject();
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle == null)
            {
                return true;
            }
            if (!(vehicle is Drone) && __instance.isValidHandTarget && !dockable.isInTransition)
            {
                vehicle.Undock();
            }
            return false;
        }
    }
}
