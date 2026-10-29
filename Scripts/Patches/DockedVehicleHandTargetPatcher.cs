using System.Linq;
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
            // BZ compatibility: bays expose a Dockable instead of GetDockedVehicle; retain the original framework hover text.
            Dockable dockable = __instance.dockingBay.GetDockedObject();
            ModVehicle? mv = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (mv != null)
            {
                string text = mv.HullName;
                if(mv is Submarine sub && sub.Hatches.Count > 0)
                {
                    text = sub.Hatches.First().Hatch.GetComponent<VehicleChildComponents.VehicleHatch>().EnterHint;
                }
                float energyActual = 0;
                float energyMax = 0;
                foreach (EnergyMixin battery in mv.energyInterface.sources)
                {
                    energyActual += battery.charge;
                    energyMax += battery.capacity;
                }
                float energyFraction = energyActual / energyMax;
                if (energyFraction == 1)
                {
                    string format2 = Language.main.GetFormat<float>("VehicleStatusChargedFormat", mv.liveMixin.GetHealthFraction());
                    HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, ((mv as Drone) == null) ? GameInput.Button.LeftHand : GameInput.Button.None);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format2, false, GameInput.Button.None);
                }
                else
                {
                    string format = Language.main.GetFormat<float, float>("VehicleStatusFormat", mv.liveMixin.GetHealthFraction(), energyFraction);
                    HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, ((mv as Drone) == null) ? GameInput.Button.LeftHand : GameInput.Button.None);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format, false, GameInput.Button.None);
                }
                HandReticle.main.SetIcon(((mv as Drone) == null) ? HandReticle.IconType.Hand : HandReticle.IconType.Default, 1f);
            }
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
